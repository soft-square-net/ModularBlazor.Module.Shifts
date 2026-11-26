using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using soft.square.net.Organization.Contracts;
using soft.square.net.Shifts.Contracts;
using soft.square.net.WebApi.Contracts.Live;
using soft.square.net.WebApi.Modules.Shifts.Infrastructure.Data;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;

namespace soft.square.net.WebApi.Modules.Shifts.Infrastructure.Live;

/// <summary>
/// <see cref="IShiftLive"/> over the platform's <see cref="ILiveNotifier"/> (in the current tenant): the shift's topic
/// <c>shifts:shift:{id}</c>, then <c>PublishToUnitAsync(unit.Path, "shift.changed", {shiftId})</c>, which reaches the
/// unit's topic and every ancestor's. Best effort: without a notifier (tests, tools) nothing is sent, and a failure is
/// logged, never thrown (the change is committed already).
/// </summary>
public sealed class ShiftLive(IServiceProvider services, ILogger<ShiftLive> log) : IShiftLive
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task ChangedAsync(int shiftId, string? unitPath, CancellationToken ct)
    {
        if (services.GetService<ILiveNotifier>() is not { } live) return;
        var payload = JsonSerializer.SerializeToElement(new ShiftChanged(shiftId), Json);
        try
        {
            await live.PublishAsync(ShiftsLive.Topic(shiftId), ShiftsLive.ChangedMessage, payload, sequence: null, ct);
            if (!string.IsNullOrWhiteSpace(unitPath)) await live.PublishToUnitAsync(unitPath, ShiftsLive.ChangedMessage, payload, sequence: null, ct);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            log.LogDebug(e, "Live message for shift {Shift} was not sent.", shiftId);
        }
    }
}

/// <summary>
/// The module's <see cref="ILiveScopeResolver"/> for its topic kind <see cref="ShiftsLive.Kind"/> (declared as
/// <c>LiveTopic("shift", "shifts.view")</c>): the scopes of shift <c>{id}</c> beyond <c>shift:{id}</c> are its unit's and
/// every ancestor's (<c>unit:{id}</c>, <see cref="UnitScopes.For"/>), so <c>shifts.view</c> on a Department covers the
/// shifts of its Sections. Server-side data only; an unknown shift, or one with no cached unit, has none (fails closed).
/// </summary>
public sealed class ShiftLiveScopes(ShiftsDbContext db) : ILiveScopeResolver
{
    public async Task<IReadOnlyList<string>> ScopesAsync(string kind, string key, CancellationToken ct)
    {
        if (kind != ShiftsLive.Kind || !int.TryParse(key, out var shiftId)) return [];
        var unitId = await db.Shifts.AsNoTracking().Where(s => s.Id == shiftId).Select(s => s.UnitId).FirstOrDefaultAsync(ct);
        if (unitId is null) return [];
        var path = await db.Units.AsNoTracking().Where(u => u.Id == unitId && !u.Removed).Select(u => u.Path).FirstOrDefaultAsync(ct);
        return path is null ? [] : UnitScopes.For(path);
    }
}
