using soft.square.net.WebApi.Modules.Shifts.UseCases.Units;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Units;

/// <summary>GET /units: the Organization units the module knows (its cache), by path.</summary>
public sealed class List : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet("/units", HandleAsync)
            .RequirePermission(ShiftPermissions.View, ShiftPermissions.Manage)
            .WithName("Shifts_ListUnits")
            .WithSummary("The tenant's Organization units as this module caches them (from org events and the unit directory), by path.");

    private static async Task<Results<Ok<IReadOnlyList<UnitDto>>, ProblemHttpResult>> HandleAsync(ISender sender, CancellationToken ct) =>
        (await sender.Send(new ListUnitsQuery(), ct)).ToOkResult();
}
