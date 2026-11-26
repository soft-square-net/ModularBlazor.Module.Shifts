using soft.square.net.WebApi.Modules.Shifts.UseCases.Roles;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Roles;

/// <summary>GET /roles/effective?unitId=: the roles that hold for a unit (the nearest definition of each key up its path).</summary>
public sealed class ListEffective : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet("/roles/effective", HandleAsync)
            .RequirePermission(ShiftPermissions.View, ShiftPermissions.Manage)
            .WithName("Shifts_ListEffectiveRoles")
            .WithSummary("The roles a rota of the unit can use: for each key, the definition nearest to the unit up its path (the unit's, a parent's, else the tenant's). Without unitId: the tenant-wide ones.");

    private static async Task<Results<Ok<IReadOnlyList<RoleDefinitionDto>>, ProblemHttpResult>> HandleAsync(Guid? unitId, ISender sender,
        CancellationToken ct) =>
        (await sender.Send(new ListEffectiveRolesQuery(unitId), ct)).ToOkResult();
}
