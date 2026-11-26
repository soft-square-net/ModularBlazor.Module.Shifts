using soft.square.net.WebApi.Modules.Shifts.UseCases.Roles;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Roles;

/// <summary>DELETE /roles/{id}: remove a role definition.</summary>
public sealed class Delete : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/roles/{id:int}", HandleAsync)
            .RequirePermission(ShiftPermissions.Manage)
            .WithName("Shifts_DeleteRole")
            .WithSummary("Removes a role definition. Refused (409) while shifts that have not ended or templates use it, unless another definition of the same key and lead flag (a parent unit's or the tenant's) takes over for them.");

    private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(int id, ISender sender, CancellationToken ct) =>
        (await sender.Send(new DeleteRoleDefinitionCommand(id), ct)).ToNoContentResult();
}
