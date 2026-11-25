using soft.square.net.WebApi.Modules.Shifts.UseCases.Sections;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Sections.Update;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Sections;

/// <summary>PUT /sections/{id}: change a section; its published shifts' grants follow.</summary>
public sealed class Update : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapPut("/sections/{id:int}", HandleAsync)
            .RequirePermission(ShiftPermissions.Manage)
            .WithName("Shifts_UpdateSection")
            .WithSummary("Changing the categories or the kind rewrites the grants of the section's published shifts that have not ended.");

    private static async Task<Results<Ok<SectionDto>, ProblemHttpResult>> HandleAsync(int id, SectionRequest r, ISender sender, CancellationToken ct) =>
        (await sender.Send(new UpdateSectionCommand(id, r.Name, r.Categories, r.AutoAssignDelayMinutes, r.CheckoutGraceMinutes, r.Kind), ct)).ToOkResult();
}
