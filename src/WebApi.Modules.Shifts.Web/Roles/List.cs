using soft.square.net.WebApi.Modules.Shifts.UseCases.Roles;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Roles;

/// <summary>GET /roles: every role definition of the tenant (the tenant-wide ones and the units' overrides).</summary>
public sealed class List : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet("/roles", HandleAsync)
            .RequirePermission(ShiftPermissions.View, ShiftPermissions.Manage)
            .WithName("Shifts_ListRoles")
            .WithSummary("Every rota role definition of the tenant: the tenant-wide ones (unitId null) and every unit's overrides.");

    private static async Task<Results<Ok<IReadOnlyList<RoleDefinitionDto>>, ProblemHttpResult>> HandleAsync(ISender sender, CancellationToken ct) =>
        (await sender.Send(new ListRoleDefinitionsQuery(), ct)).ToOkResult();
}
