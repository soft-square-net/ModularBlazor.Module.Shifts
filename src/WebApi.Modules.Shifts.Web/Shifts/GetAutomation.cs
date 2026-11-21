using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.GetAutomation;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Shifts;

/// <summary>GET /shifts/{id}/automation: the shift's clock and timers, for the workflow service.</summary>
public sealed class GetAutomation : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet("/shifts/{id:int}/automation", HandleAsync)
            .RequirePermission(ShiftPermissions.View, ShiftPermissions.Manage)
            .WithName("Shifts_GetShiftAutomation")
            .WithSummary("Shift clock and section-specific assignment/check-out timers for the workflow service.");

    private static async Task<Results<Ok<ShiftAutomationInfo>, ProblemHttpResult>> HandleAsync(int id, ISender sender, CancellationToken ct) =>
        (await sender.Send(new GetShiftAutomationQuery(id), ct)).ToOkResult();
}
