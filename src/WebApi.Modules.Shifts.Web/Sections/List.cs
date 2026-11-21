using soft.square.net.WebApi.Modules.Shifts.UseCases.Sections;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Sections.List;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Sections;

/// <summary>GET /sections: every section of the tenant.</summary>
public sealed class List : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet("/sections", HandleAsync)
            .RequirePermission(ShiftPermissions.View, ShiftPermissions.Manage)
            .WithName("Shifts_ListSections");

    private static async Task<Results<Ok<IReadOnlyList<SectionDto>>, ProblemHttpResult>> HandleAsync(ISender sender, CancellationToken ct) =>
        (await sender.Send(new ListSectionsQuery(), ct)).ToOkResult();
}
