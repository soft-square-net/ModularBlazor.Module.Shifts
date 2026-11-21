using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.CheckOutMember;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Shifts;

/// <summary>
/// POST /shifts/{id}/members/{userId}/check-out: one member leaves early. No permission on the route: members check
/// themselves out (the handler checks who may check out whom).
/// </summary>
public sealed class CheckOutMember : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapPost("/shifts/{id:int}/members/{userId}/check-out", HandleAsync)
            .WithName("Shifts_CheckOutMember")
            .WithSummary("One member leaves early; their grants end now (themselves, the shift's manager, or a shift admin).");

    private static async Task<Results<Ok<ShiftDto>, ProblemHttpResult>> HandleAsync(int id, string userId, ISender sender, CancellationToken ct) =>
        (await sender.Send(new CheckOutMemberCommand(id, userId), ct)).ToOkResult();
}
