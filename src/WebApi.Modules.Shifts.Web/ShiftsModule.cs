using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using soft.square.net.WebApi.Contracts;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Units;
using soft.square.net.WebApi.Modules.Shifts.Web.Configurations;

namespace soft.square.net.WebApi.Modules.Shifts.Web;

/// <summary>
/// Shifts module: sections (the categories a desk covers), shifts on the clock and their rota. Rota roles are data (role
/// definitions, tenant-wide or per unit; manager, desk and editor are seeded), and units have shift templates.
/// A published rota becomes time-windowed grants in the host (source <c>shift:{id}</c>), so newsroom rights start and
/// end with the shift, or earlier on check-out. Shifts belong to Organization units, which the module follows in a cache
/// (the <c>org.unit-*</c> and <c>org.membership-changed</c> events); it publishes <c>shifts.section-moved.v1</c> when a
/// section moves to Organization. Tenant data (schema "shifts" in each tenant's database).
/// This is the composition root (the template's Program.cs): the runtime creates it, reads the manifest and calls
/// <see cref="ConfigureServices"/> on the module's own container; the endpoints are in <see cref="ShiftsRoutes"/>.
/// </summary>
public sealed class ShiftsModule : IRuntimeModule
{
    public ModuleManifest Manifest { get; } = new("shifts", "Shifts", "0.1.0", "shifts")
    {
        Description = "Newsroom shifts: units, rota roles as data, templates, check-out; the rota becomes time-windowed grants.",
        DefaultSettings = new Dictionary<string, string?>
        {
            ["Shifts:AutoAssignDelay"] = "00:05:00",
            ["Shifts:CheckoutGrace"] = "00:15:00",
            ["Shifts:Organization:BaseUrl"] = "",
            ["Shifts:Organization:CentralUrl"] = "",
            ["Shifts:Organization:TokenEndpoint"] = "",
            ["Shifts:Organization:ClientId"] = "",
            ["Shifts:Organization:ClientScope"] = ""
        },
        Permissions = ShiftPermissions.All,
        Subscriptions = [.. OrgEventsHandler.Patterns]
    };

    public void ConfigureServices(IServiceCollection services, IConfiguration settings) => services.AddServiceConfigs(settings);
}
