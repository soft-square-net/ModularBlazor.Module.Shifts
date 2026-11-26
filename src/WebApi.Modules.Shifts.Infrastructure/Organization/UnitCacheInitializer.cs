using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using soft.square.net.WebApi.Contracts.Tenancy;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Units;

namespace soft.square.net.WebApi.Modules.Shifts.Infrastructure.Organization;

/// <summary>
/// The unit cache's first fill, for each tenant when the module loads (after the tenant's migrations, which are
/// registered before it): the deployment's unit directory (<c>IUnitDirectory.ListAsync</c>), read as the module's
/// service client. It never fails the tenant: without a service client, or when Organization cannot be reached, it
/// logs and leaves the cache to the <c>org.*</c> events and to an admin's resync (<c>POST /units/resync</c>).
/// </summary>
public sealed class UnitCacheInitializer(IServiceProvider services, OrganizationHttp http, ILogger<UnitCacheInitializer> log) : ITenantInitializer
{
    public async Task InitializeAsync(TenantInfo tenant, CancellationToken ct)
    {
        if (!http.Options.CanActAsService)
        {
            log.LogInformation("Shifts:Organization has no service client: the unit cache of tenant {Tenant} fills from org events and resyncs only", tenant.Id);
            return;
        }
        try
        {
            await using var scope = services.CreateTenantScope(tenant);
            var organization = scope.ServiceProvider.GetRequiredService<IOrganizationAccess>();
            var cache = scope.ServiceProvider.GetRequiredService<UnitCache>();
            var result = await cache.ResyncAsync(await organization.DirectoryAsService().ListAsync(ct), ct);
            await cache.SaveAsync(ct);
            log.LogInformation("Unit cache of tenant {Tenant} filled: {Listed} listed, {Changed} changed, {Deleted} dropped",
                tenant.Id, result.Listed, result.Changed, result.Deleted);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            log.LogWarning(e, "The unit cache of tenant {Tenant} could not be filled from Organization; it follows org events until a resync", tenant.Id);
        }
    }
}
