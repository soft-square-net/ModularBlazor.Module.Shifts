using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.GetById;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Shifts;

/// <summary>GET /shifts/{id}: one shift with its rota.</summary>
public sealed class GetById : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet("/shifts/{id:int}", HandleAsync)
            .RequirePermission(ShiftPermissions.View, ShiftPermissions.Manage)
            .WithName("Shifts_GetShift");

    private static async Task<Results<Ok<ShiftDto>, ProblemHttpResult>> HandleAsync(int id, ISender sender, CancellationToken ct) =>
        (await sender.Send(new GetShiftByIdQuery(id), ct)).ToOkResult();
}
