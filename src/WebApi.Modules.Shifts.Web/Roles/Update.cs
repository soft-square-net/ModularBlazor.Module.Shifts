using soft.square.net.WebApi.Modules.Shifts.UseCases.Roles;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Roles;

/// <summary>PUT /roles/{id}: change a role's names, permissions and lead flag.</summary>
public sealed class Update : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapPut("/roles/{id:int}", HandleAsync)
            .RequirePermission(ShiftPermissions.Manage)
            .WithName("Shifts_UpdateRole")
            .WithSummary("Changes a role's names, permissions and lead flag (its key and unit stay). Published shifts that use it and have not ended get their grants rewritten. The lead flag cannot change while shifts or templates use the role (409).");

    private static async Task<Results<Ok<RoleDefinitionDto>, ProblemHttpResult>> HandleAsync(int id, RoleUpdateRequest r, ISender sender,
        CancellationToken ct) =>
        (await sender.Send(new UpdateRoleDefinitionCommand(id, r.NameEn, r.NameAr, r.Permissions ?? [], r.IsLead), ct)).ToOkResult();
}
