using soft.square.net.WebApi.Modules.Shifts.UseCases.Templates;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Templates;

/// <summary>GET /templates?unitId=: the shift templates, optionally of one unit.</summary>
public sealed class List : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet("/templates", HandleAsync)
            .RequirePermission(ShiftPermissions.View, ShiftPermissions.Manage)
            .WithName("Shifts_ListTemplates");

    private static async Task<Results<Ok<IReadOnlyList<ShiftTemplateDto>>, ProblemHttpResult>> HandleAsync(Guid? unitId, ISender sender,
        CancellationToken ct) =>
        (await sender.Send(new ListTemplatesQuery(unitId), ct)).ToOkResult();
}
