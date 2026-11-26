using soft.square.net.WebApi.Modules.Shifts.UseCases.Sections.Export;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Sections;

/// <summary>GET /sections/export: the sections as JSON, with the Section unit to create for each, for a move by hand.</summary>
public sealed class Export : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet("/sections/export", HandleAsync)
            .RequirePermission(ShiftPermissions.Manage)
            .WithName("Shifts_ExportSections")
            .WithSummary("Every section with the Section unit to create for it in Organization and the unit it moved to, if any. Create the units, " +
                         "then link them with POST /sections/move-to-organization (links).");

    private static async Task<Results<Ok<SectionsExport>, ProblemHttpResult>> HandleAsync(ISender sender, CancellationToken ct) =>
        (await sender.Send(new ExportSectionsQuery(), ct)).ToOkResult();
}
