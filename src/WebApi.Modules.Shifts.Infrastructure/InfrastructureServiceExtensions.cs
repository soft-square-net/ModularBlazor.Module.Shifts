using Microsoft.Extensions.DependencyInjection;
using soft.square.net.WebApi.Contracts.Messaging;
using soft.square.net.WebApi.Contracts.Tenancy;
using soft.square.net.WebApi.Data;
using soft.square.net.WebApi.Modules.Shifts.Infrastructure.Data;
using soft.square.net.WebApi.Modules.Shifts.Infrastructure.Organization;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Units;

namespace soft.square.net.WebApi.Modules.Shifts.Infrastructure;

public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// The module's data access and its links to the platform and to Organization, in the module's own container:
    /// <list type="bullet">
    /// <item>the tenant DbContext, and through it <c>IRepository&lt;T&gt;</c> / <c>IReadRepository&lt;T&gt;</c> for every
    /// aggregate root and the domain event dispatcher (the connection comes from the tenant);</item>
    /// <item>integration events: the outbox (<c>shifts.*</c>) and the inbox with the <c>org.*</c> handler
    /// (<see cref="OrgEventsHandler.Patterns"/>, also in the manifest's Subscriptions);</item>
    /// <item>the unit cache's store, the HTTP access to Organization (settings <c>Shifts:Organization</c>) and the cache's
    /// first fill per tenant, registered after the migrations so it runs on a migrated database;</item>
    /// <item>the default role definitions, seeded per tenant (also after the migrations).</item>
    /// </list>
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddTenantDbContext<ShiftsDbContext>();
        services.AddIntegrationEvents<ShiftsDbContext>(o => o.Source = "shifts");
        foreach (var pattern in OrgEventsHandler.Patterns) services.AddIntegrationHandler<OrgEventsHandler>(pattern);

        services.AddScoped<IUnitCacheStore, EfUnitCacheStore>();
        services.AddSingleton<OrganizationHttp>();
        services.AddScoped<IOrganizationAccess, OrganizationAccess>();
        services.AddSingleton<ITenantInitializer, UnitCacheInitializer>();
        services.AddSingleton<ITenantInitializer, ShiftRolesInitializer>();
        return services;
    }
}
