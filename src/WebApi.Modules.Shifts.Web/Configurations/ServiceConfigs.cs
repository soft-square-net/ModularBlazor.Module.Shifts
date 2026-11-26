using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using soft.square.net.WebApi.Modules.Shifts.Infrastructure;
using soft.square.net.WebApi.Modules.Shifts.Infrastructure.Organization;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Authority;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Roles;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Templates;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Units;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Configurations;

public static class ServiceConfigs
{
    /// <summary>
    /// Everything the module registers, in the module's own container (never the host's): settings, data access, the
    /// services the use cases share, and the mediator with every handler.
    /// </summary>
    public static IServiceCollection AddServiceConfigs(this IServiceCollection services, IConfiguration settings)
    {
        services.Configure<ShiftOptions>(settings.GetSection("Shifts"));
        services.Configure<OrganizationOptions>(settings.GetSection("Shifts:Organization"));
        services.AddHttpContextAccessor();
        services.AddInfrastructureServices();

        // The unit cache (Organization's units, as this module follows them).
        services.AddScoped<UnitCache>();

        // Shared by the use cases.
        services.AddScoped<ShiftCaller>();
        services.AddScoped<ShiftGrants>();
        services.AddScoped<ShiftDetails>();
        services.AddScoped<ShiftChanges>();
        services.AddScoped<ShiftUnits>();
        services.AddScoped<ShiftAuthority>();

        // Roles as data, the rota's checks, and templates.
        services.AddScoped<ShiftRoles>();
        services.AddScoped<UnitMembers>();
        services.AddScoped<ShiftRota>();
        services.AddScoped<RoleUsage>();
        services.AddScoped<RoleGrants>();
        services.AddScoped<RolePermissions>();
        services.AddScoped<SeedShiftRoles>();
        services.AddScoped<TemplateRules>();

        services.AddMediatorSourceGen();
        return services;
    }
}
