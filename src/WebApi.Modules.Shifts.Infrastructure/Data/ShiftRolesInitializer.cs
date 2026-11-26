using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using soft.square.net.WebApi.Contracts.Tenancy;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Roles;

namespace soft.square.net.WebApi.Modules.Shifts.Infrastructure.Data;

/// <summary>
/// Seeds the default role definitions (manager, desk, editor) into each tenant that has none, when the module loads and
/// when a tenant is added. Registered after the migrations, so the table exists. Existing rotas already hold these keys
/// (the <c>RolesAndTemplates</c> migration turned the old role names into them).
/// </summary>
public sealed class ShiftRolesInitializer(IServiceProvider services, ILogger<ShiftRolesInitializer> log) : ITenantInitializer
{
    public async Task InitializeAsync(TenantInfo tenant, CancellationToken ct)
    {
        await using var scope = services.CreateTenantScope(tenant);
        if (await scope.ServiceProvider.GetRequiredService<SeedShiftRoles>().SeedAsync(ct))
            log.LogInformation("Seeded the default shift roles of tenant {Tenant}", tenant.Id);
    }
}
