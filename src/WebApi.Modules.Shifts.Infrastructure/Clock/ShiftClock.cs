using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using soft.square.net.WebApi.Contracts.Tenancy;
using soft.square.net.WebApi.Data;
using soft.square.net.WebApi.Modules.Shifts.UseCases;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Attendance;

namespace soft.square.net.WebApi.Modules.Shifts.Infrastructure.Clock;

/// <summary>
/// One clock tick of a tenant at a time, across every replica of every pool that runs the module: a try-lock, so a
/// replica that does not get it skips the tenant for this tick (another one is running it).
/// </summary>
public interface IShiftClockLock
{
    /// <summary>The lock, held until disposed; null when another replica holds it.</summary>
    Task<IAsyncDisposable?> TryAcquireAsync(TenantInfo tenant, CancellationToken ct);
}

/// <summary>The lock in the tenant's own database: the platform's advisory lock (as for migrations), without waiting.</summary>
public sealed class TenantDatabaseClockLock(TenantDatabases databases) : IShiftClockLock
{
    public const string LockName = "shifts:clock";

    public Task<IAsyncDisposable?> TryAcquireAsync(TenantInfo tenant, CancellationToken ct)
    {
        var server = databases.ServerOf(tenant);
        var connection = server.Adapter.LockConnectionString(server.ConnectionString, databases.ConnectionString(tenant));
        return server.Adapter.TryLockAsync(connection, $"{tenant.DatabaseName}:{LockName}", ct);
    }
}

/// <summary>
/// The tenants the module serves on this replica: added when the module's tenant initializers have run for one
/// (registered after the migrations, so its database has the module's tables), so the clock never queries a tenant
/// whose database is not migrated yet.
/// </summary>
public sealed class ShiftClockTenants : ITenantInitializer
{
    private readonly ConcurrentDictionary<string, bool> _ready = new(StringComparer.OrdinalIgnoreCase);

    public Task InitializeAsync(TenantInfo tenant, CancellationToken ct)
    {
        _ready[tenant.Id] = true;
        return Task.CompletedTask;
    }

    public bool IsReady(string tenantId) => _ready.ContainsKey(tenantId);
}

/// <summary>
/// The module's clock (a hosted service of the module's container, stopped on unload). Every
/// <see cref="ShiftOptions.ClockInterval"/> (a minute) it runs <see cref="ShiftClockRun.TickAsync"/> for every active
/// tenant the module serves, in a tenant scope, under <see cref="IShiftClockLock"/>: absences, automatic check-outs at
/// the end plus the grace, and ends (<c>shifts.ended.v1</c>). The first tick of each tenant after the module starts
/// also writes the grants of its running shifts again, so the check-in rule holds for shifts published by an older
/// version. A tenant that fails is logged and tried again at the next tick; the others are not held up.
/// </summary>
public sealed class ShiftClock(
    IServiceProvider services, ITenantDirectory tenants, ShiftClockTenants ready, IShiftClockLock locks, IOptions<ShiftOptions> options,
    ILogger<ShiftClock> log) : BackgroundService
{
    private readonly ConcurrentDictionary<string, bool> _grantsRewritten = new(StringComparer.OrdinalIgnoreCase);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = options.Value.ClockInterval > TimeSpan.Zero ? options.Value.ClockInterval : TimeSpan.FromMinutes(1);
        using var timer = new PeriodicTimer(interval);
        do
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception e)
            {
                log.LogWarning(e, "The shifts clock's tick failed; retrying in {Interval}.", interval);
            }
        } while (await WaitAsync(timer, stoppingToken));
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try { return await timer.WaitForNextTickAsync(ct); }
        catch (OperationCanceledException) { return false; }
    }

    /// <summary>One tick: every active tenant the module serves.</summary>
    public async Task RunOnceAsync(CancellationToken ct)
    {
        foreach (var tenant in tenants.All.Where(t => t.IsActive && ready.IsReady(t.Id)))
            await TickAsync(tenant, ct);
    }

    /// <summary>One tenant's tick, when this replica gets the tenant's lock. Never throws but on cancellation.</summary>
    public async Task<ClockTick?> TickAsync(TenantInfo tenant, CancellationToken ct)
    {
        try
        {
            await using var held = await locks.TryAcquireAsync(tenant, ct);
            if (held is null) return null; // another replica runs this tenant's tick
            await using var scope = services.CreateTenantScope(tenant);
            var run = scope.ServiceProvider.GetRequiredService<ShiftClockRun>();
            if (!_grantsRewritten.ContainsKey(tenant.Id))
            {
                var rewritten = await run.RewriteGrantsAsync(ct);
                _grantsRewritten[tenant.Id] = true;
                if (rewritten > 0) log.LogInformation("Wrote the grants of {Count} running shift(s) of tenant {Tenant} again.", rewritten, tenant.Id);
            }
            var tick = await run.TickAsync(ct);
            if (tick.Changed)
                log.LogInformation("Shifts clock, tenant {Tenant}: {Absent} absent, {CheckedOut} checked out, {Ended} ended.",
                    tenant.Id, tick.Absent, tick.CheckedOut, tick.Ended);
            return tick;
        }
        catch (Exception e) when (e is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            log.LogWarning(e, "The shifts clock could not run for tenant {Tenant}; it tries again at the next tick.", tenant.Id);
            return null;
        }
    }
}
