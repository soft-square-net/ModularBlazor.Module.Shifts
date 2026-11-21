using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.CheckOut;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Shifts;

/// <summary>
/// POST /shifts/{id}/check-out: handover. No permission on the route: the shift's manager may do it without one (the
/// handler checks), a shift admin with <see cref="ShiftPermissions.Manage"/>.
/// </summary>
public sealed class CheckOut : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapPost("/shifts/{id:int}/check-out", HandleAsync)
            .WithName("Shifts_CheckOutShift")
            .WithSummary("Handover: the whole shift checks out and every member's grants end now (the shift's manager, or a shift admin).");

    private static async Task<Results<Ok<ShiftDto>, ProblemHttpResult>> HandleAsync(int id, ISender sender, CancellationToken ct) =>
        (await sender.Send(new CheckOutShiftCommand(id), ct)).ToOkResult();
}
