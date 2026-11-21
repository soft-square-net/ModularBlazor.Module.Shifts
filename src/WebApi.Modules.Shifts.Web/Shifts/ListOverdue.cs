using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.ListOverdue;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Shifts;

/// <summary>GET /overdue: published shifts past their end that nobody checked out.</summary>
public sealed class ListOverdue : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet("/overdue", HandleAsync)
            .RequirePermission(ShiftPermissions.Manage)
            .WithName("Shifts_ListOverdue")
            .WithSummary("Published shifts past their end that nobody checked out (for the workflow's automatic check-out).");

    private static async Task<Results<Ok<IReadOnlyList<ShiftDto>>, ProblemHttpResult>> HandleAsync(ISender sender, CancellationToken ct) =>
        (await sender.Send(new ListOverdueShiftsQuery(), ct)).ToOkResult();
}
