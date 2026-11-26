using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.ListEnded;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Shifts;

/// <summary>GET /shifts/ended?since={utc}&amp;take=: shifts that ended at or after <c>since</c>, by the time they ended.</summary>
public sealed class ListEnded : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet("/shifts/ended", HandleAsync)
            .RequirePermission(ShiftPermissions.Manage)
            .WithName("Shifts_ListEnded")
            .WithSummary("Shifts that ended (checked out as a whole, or by the clock) at or after since, ordered by the time they ended (for the workflow's handover).");

    private static async Task<Results<Ok<IReadOnlyList<EndedShiftDto>>, ProblemHttpResult>> HandleAsync(DateTimeOffset since, int? take, ISender sender,
        CancellationToken ct) =>
        (await sender.Send(new ListEndedShiftsQuery(since, take), ct)).ToOkResult();
}
