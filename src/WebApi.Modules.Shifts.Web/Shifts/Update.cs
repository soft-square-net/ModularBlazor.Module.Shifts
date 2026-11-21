using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Update;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Shifts;

/// <summary>PUT /shifts/{id}: reschedule a shift and replace its rota.</summary>
public sealed class Update : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapPut("/shifts/{id:int}", HandleAsync)
            .RequirePermission(ShiftPermissions.Manage)
            .WithName("Shifts_UpdateShift");

    private static async Task<Results<Ok<ShiftDto>, ProblemHttpResult>> HandleAsync(int id, ShiftRequest r, ISender sender, CancellationToken ct) =>
        (await sender.Send(new UpdateShiftCommand(id, r.SectionId, r.StartsAt, r.EndsAt, r.Rota), ct)).ToOkResult();
}
