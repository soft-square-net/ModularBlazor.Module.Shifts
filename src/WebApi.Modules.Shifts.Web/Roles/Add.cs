using soft.square.net.WebApi.Modules.Shifts.UseCases.Roles;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Roles;

/// <summary>POST /roles: add a role definition, tenant-wide or for a unit.</summary>
public sealed class Add : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapPost("/roles", HandleAsync)
            .RequirePermission(ShiftPermissions.Manage)
            .WithName("Shifts_AddRole")
            .WithSummary("Adds a rota role: its key (a lowercase slug), names, the permissions it grants while a shift runs (from the platform's permission catalog) and whether it is the shift's lead. Tenant-wide (no unitId; needs shifts.manage tenant-wide) or for a unit and the units below it (needs shifts.manage on the unit).");

    private static async Task<Results<Created<RoleDefinitionDto>, ProblemHttpResult>> HandleAsync(RoleRequest r, ISender sender, CancellationToken ct) =>
        (await sender.Send(new AddRoleDefinitionCommand(r.Key, r.NameEn, r.NameAr, r.Permissions ?? [], r.IsLead, r.UnitId), ct))
            .ToCreatedResult(role => $"roles/{role.Id}");
}
