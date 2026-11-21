using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using soft.square.net.WebApi.Modules.Shifts.Infrastructure;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;

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
        services.AddInfrastructureServices();

        // Shared by the use cases.
        services.AddScoped<ShiftCaller>();
        services.AddScoped<ShiftGrants>();
        services.AddScoped<ShiftDetails>();
        services.AddScoped<ShiftChanges>();

        services.AddMediatorSourceGen();
        return services;
    }
}
