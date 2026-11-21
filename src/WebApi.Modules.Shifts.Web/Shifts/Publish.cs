using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Publish;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Shifts;

/// <summary>POST /shifts/{id}/publish: write the rota's grants.</summary>
public sealed class Publish : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapPost("/shifts/{id:int}/publish", HandleAsync)
            .RequirePermission(ShiftPermissions.Manage)
            .WithName("Shifts_PublishShift")
            .WithSummary("Writes the rota's grants: each member's role permissions on the section's categories, valid for the shift only.");

    private static async Task<Results<Ok<ShiftDto>, ProblemHttpResult>> HandleAsync(int id, ISender sender, CancellationToken ct) =>
        (await sender.Send(new PublishShiftCommand(id), ct)).ToOkResult();
}
