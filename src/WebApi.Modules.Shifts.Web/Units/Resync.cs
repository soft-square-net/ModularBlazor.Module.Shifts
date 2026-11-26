using soft.square.net.WebApi.Modules.Shifts.UseCases.Units;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Units;

/// <summary>POST /units/resync: refill the unit cache from the deployment's unit directory, as the caller.</summary>
public sealed class Resync : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapPost("/units/resync", HandleAsync)
            .RequirePermission(ShiftPermissions.Manage)
            .WithName("Shifts_ResyncUnits")
            .WithSummary("Reads the tenant's units from Organization's local directory (with the caller's token) and replaces the cache: newer versions are applied, units no longer listed are dropped.");

    private static async Task<Results<Ok<UnitResyncResult>, ProblemHttpResult>> HandleAsync(ISender sender, CancellationToken ct) =>
        (await sender.Send(new ResyncUnitsCommand(), ct)).ToOkResult();
}
