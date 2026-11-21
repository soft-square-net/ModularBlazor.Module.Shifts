using soft.square.net.WebApi.Modules.Shifts.UseCases.Sections;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Sections.GetSettings;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Sections;

/// <summary>GET /sections/{id}/settings: the section's effective timers.</summary>
public sealed class GetSettings : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet("/sections/{id:int}/settings", HandleAsync)
            .RequirePermission(ShiftPermissions.View, ShiftPermissions.Manage)
            .WithName("Shifts_GetSectionSettings")
            .WithSummary("Effective AutoAssignDelay and CheckoutGrace: the section's override, else the module's settings.");

    private static async Task<Results<Ok<SectionSettings>, ProblemHttpResult>> HandleAsync(int id, ISender sender, CancellationToken ct) =>
        (await sender.Send(new GetSectionSettingsQuery(id), ct)).ToOkResult();
}
