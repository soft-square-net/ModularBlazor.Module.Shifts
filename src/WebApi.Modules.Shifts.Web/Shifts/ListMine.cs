using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.ListMine;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Shifts;

/// <summary>GET /mine: the caller's running and upcoming shifts (any signed-in user).</summary>
public sealed class ListMine : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet("/mine", HandleAsync)
            .WithName("Shifts_ListMine")
            .WithSummary("Your running and upcoming shifts (next 7 days).");

    private static async Task<Results<Ok<IReadOnlyList<ShiftDto>>, ProblemHttpResult>> HandleAsync(ISender sender, CancellationToken ct) =>
        (await sender.Send(new ListMyShiftsQuery(), ct)).ToOkResult();
}
