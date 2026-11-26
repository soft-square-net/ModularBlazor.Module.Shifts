using soft.square.net.WebApi.Modules.Shifts.UseCases.Templates;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Templates;

/// <summary>PUT /templates/{id}: change a shift template (it stays on its unit).</summary>
public sealed class Update : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapPut("/templates/{id:int}", HandleAsync)
            .RequirePermission(ShiftPermissions.Manage)
            .WithName("Shifts_UpdateTemplate");

    private static async Task<Results<Ok<ShiftTemplateDto>, ProblemHttpResult>> HandleAsync(int id, TemplateRequest r, ISender sender,
        CancellationToken ct) =>
        (await sender.Send(new UpdateTemplateCommand(id, r.Name, r.StartLocal, r.Duration, r.Slots, r.DefaultMembers), ct)).ToOkResult();
}
