using soft.square.net.WebApi.Modules.Shifts.UseCases.Templates;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Templates;

/// <summary>POST /templates: add a shift template to a unit.</summary>
public sealed class Add : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapPost("/templates", HandleAsync)
            .RequirePermission(ShiftPermissions.Manage)
            .WithName("Shifts_AddTemplate")
            .WithSummary("Adds a shift template to a unit (needs shifts.manage on the unit): start (wall-clock, unit time zone), duration (at most 24h), role slots and default members (members of the unit or a unit above it; at most one in a lead role).");

    private static async Task<Results<Created<ShiftTemplateDto>, ProblemHttpResult>> HandleAsync(TemplateRequest r, ISender sender, CancellationToken ct) =>
        (await sender.Send(new AddTemplateCommand(r.UnitId, r.Name, r.StartLocal, r.Duration, r.Slots, r.DefaultMembers), ct))
            .ToCreatedResult(template => $"templates/{template.Id}");
}
