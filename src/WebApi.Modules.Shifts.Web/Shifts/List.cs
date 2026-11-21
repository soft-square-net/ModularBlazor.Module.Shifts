using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.List;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Shifts;

/// <summary>GET /shifts?sectionId=&amp;from=&amp;to=: shifts in a window (default: a day ago to a week ahead).</summary>
public sealed class List : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet("/shifts", HandleAsync)
            .RequirePermission(ShiftPermissions.View, ShiftPermissions.Manage)
            .WithName("Shifts_ListShifts");

    private static async Task<Results<Ok<IReadOnlyList<ShiftDto>>, ProblemHttpResult>> HandleAsync(
        int? sectionId, DateTimeOffset? from, DateTimeOffset? to, ISender sender, CancellationToken ct) =>
        (await sender.Send(new ListShiftsQuery(sectionId, from, to), ct)).ToOkResult();
}
