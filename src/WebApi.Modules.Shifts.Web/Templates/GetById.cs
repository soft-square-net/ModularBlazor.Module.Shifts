using soft.square.net.WebApi.Modules.Shifts.UseCases.Templates;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Templates;

/// <summary>GET /templates/{id}: one shift template.</summary>
public sealed class GetById : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet("/templates/{id:int}", HandleAsync)
            .RequirePermission(ShiftPermissions.View, ShiftPermissions.Manage)
            .WithName("Shifts_GetTemplate");

    private static async Task<Results<Ok<ShiftTemplateDto>, ProblemHttpResult>> HandleAsync(int id, ISender sender, CancellationToken ct) =>
        (await sender.Send(new GetTemplateQuery(id), ct)).ToOkResult();
}
