using soft.square.net.WebApi.Modules.Shifts.UseCases.Sections;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Sections.Serving;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Sections;

/// <summary>GET /sections/serving?category={id}: the sections that serve a category (its own, then the departments).</summary>
public sealed class ListServing : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet("/sections/serving", HandleAsync)
            .RequirePermission(ShiftPermissions.View, ShiftPermissions.Manage)
            .WithName("Shifts_ListServingSections")
            .WithSummary("The sections that serve a category: the category's own sections first, then every department (they serve all categories).");

    private static async Task<Results<Ok<IReadOnlyList<SectionDto>>, ProblemHttpResult>> HandleAsync(string category, ISender sender, CancellationToken ct) =>
        (await sender.Send(new ListServingSectionsQuery(category), ct)).ToOkResult();
}
