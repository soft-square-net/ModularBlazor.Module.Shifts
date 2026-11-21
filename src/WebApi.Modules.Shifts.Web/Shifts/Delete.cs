using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Delete;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Shifts;

/// <summary>DELETE /shifts/{id}: delete a shift and its grants.</summary>
public sealed class Delete : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/shifts/{id:int}", HandleAsync)
            .RequirePermission(ShiftPermissions.Manage)
            .WithName("Shifts_DeleteShift");

    private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(int id, ISender sender, CancellationToken ct) =>
        (await sender.Send(new DeleteShiftCommand(id), ct)).ToNoContentResult();
}
