using soft.square.net.WebApi.Modules.Shifts.UseCases.Templates;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Templates;

/// <summary>POST /templates/{id}/clone: a Draft shift from the template on a day.</summary>
public sealed class Clone : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapPost("/templates/{id:int}/clone", HandleAsync)
            .RequirePermission(ShiftPermissions.Manage)
            .WithName("Shifts_CloneTemplate")
            .WithSummary("Creates a Draft (unpublished) shift from the template on a calendar day of the unit's time zone, converted to UTC safely across daylight-saving changes. The rota is the default members; unfilled slots, a missing lead and default members who may no longer be on the rota come back as warnings.");

    private static async Task<Results<Created<TemplateCloneResult>, ProblemHttpResult>> HandleAsync(int id, TemplateCloneRequest r, ISender sender,
        CancellationToken ct) =>
        (await sender.Send(new CloneTemplateCommand(id, r.Date), ct)).ToCreatedResult(clone => $"shifts/{clone.Shift.Id}");
}
