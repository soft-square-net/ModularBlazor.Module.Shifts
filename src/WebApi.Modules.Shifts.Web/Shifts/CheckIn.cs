using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.CheckIn;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Shifts;

/// <summary>
/// POST /shifts/{id}/check-in: the caller checks in; POST /shifts/{id}/members/{userId}/check-in: the shift's lead or a
/// manager of its unit checks a member in. No permission on the routes: the handler checks who may check in whom.
/// </summary>
public sealed class CheckIn : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapPost("/shifts/{id:int}/check-in", SelfAsync)
            .WithName("Shifts_CheckIn")
            .WithSummary("The caller checks in to a shift of theirs: their grants start now (from the early check-in threshold before the start until the end).");

    public static RouteHandlerBuilder MapMember(IEndpointRouteBuilder app) =>
        app.MapPost("/shifts/{id:int}/members/{userId}/check-in", MemberAsync)
            .WithName("Shifts_CheckInMember")
            .WithSummary("The shift's lead or a manager of its unit checks a member in: the member's grants start now.");

    private static async Task<Results<Ok<ShiftDto>, ProblemHttpResult>> SelfAsync(int id, ISender sender, CancellationToken ct) =>
        (await sender.Send(new CheckInCommand(id), ct)).ToOkResult();

    private static async Task<Results<Ok<ShiftDto>, ProblemHttpResult>> MemberAsync(int id, string userId, ISender sender, CancellationToken ct) =>
        (await sender.Send(new CheckInCommand(id, userId), ct)).ToOkResult();
}
