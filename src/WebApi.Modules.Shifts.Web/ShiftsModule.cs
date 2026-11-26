using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using soft.square.net.Shifts.Contracts;
using soft.square.net.WebApi.Contracts;
using soft.square.net.WebApi.Contracts.Live;
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
        Description = "Newsroom shifts: units, rota roles as data, templates, attendance; checking in grants the role's rights until check-out.",
        DefaultSettings = new Dictionary<string, string?>
        {
            ["Shifts:AutoAssignDelay"] = "00:05:00",
            ["Shifts:CheckoutGrace"] = "00:15:00",
            ["Shifts:EarlyCheckIn"] = "00:30:00",
            ["Shifts:LateAfter"] = "00:10:00",
            ["Shifts:AbsentAfter"] = "01:00:00",
            ["Shifts:OvertimeAfter"] = "00:15:00",
            ["Shifts:ClockInterval"] = "00:01:00",
            ["Shifts:Organization:BaseUrl"] = "",
            ["Shifts:Organization:CentralUrl"] = "",
            ["Shifts:Organization:TokenEndpoint"] = "",
            ["Shifts:Organization:ClientId"] = "",
            ["Shifts:Organization:ClientScope"] = ""
        },
        Permissions = ShiftPermissions.All,
        Subscriptions = [.. OrgEventsHandler.Patterns],
        // shifts:shift:{id}: a nudge after any change of the shift; shifts.view on shift:{id}, on its unit or an ancestor
        // (ShiftLiveScopes), or tenant-wide.
        LiveTopics = [new LiveTopic(ShiftsLive.Kind, ShiftPermissions.View)]
    };

    public void ConfigureServices(IServiceCollection services, IConfiguration settings) => services.AddServiceConfigs(settings);
}
