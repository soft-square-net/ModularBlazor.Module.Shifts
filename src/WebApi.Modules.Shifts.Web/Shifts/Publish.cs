using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Publish;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Shifts;

/// <summary>POST /shifts/{id}/publish: members may check in from now on (publishing grants nobody anything).</summary>
public sealed class Publish : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapPost("/shifts/{id:int}/publish", HandleAsync)
            .RequirePermission(ShiftPermissions.Manage)
            .WithName("Shifts_PublishShift")
            .WithSummary("Publishes the shift (shifts.published.v1): its members may now check in; each gets their role permissions from check-in to check-out.");

    private static async Task<Results<Ok<ShiftDto>, ProblemHttpResult>> HandleAsync(int id, ISender sender, CancellationToken ct) =>
        (await sender.Send(new PublishShiftCommand(id), ct)).ToOkResult();
}
