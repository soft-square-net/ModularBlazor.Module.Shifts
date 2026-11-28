using soft.square.net.WebApi.Modules.Shifts.UseCases.Sections.Move;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Sections;

/// <summary>POST /sections/move-to-organization: every section becomes a Section unit of Organization (idempotent).</summary>
public sealed class MoveToOrganization : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapPost("/sections/move-to-organization", HandleAsync)
            .RequirePermission(ShiftPermissions.Manage)
            .WithName("Shifts_MoveSectionsToOrganization")
            .WithSummary("Creates a Section unit under the Department parentUnitId for each section (with the caller's token; a department section " +
                         "gets the tag 'department' and the category '*'), or links the units given in links. Maps the section, moves its shifts to the " +
                         "unit and publishes shifts.section-moved.v1. Idempotent; one result per section.");

    private static async Task<Results<Ok<SectionMoveReport>, ProblemHttpResult>> HandleAsync(MoveSectionsRequest r, ISender sender, CancellationToken ct) =>
        (await sender.Send(new MoveSectionsToOrganizationCommand(r.ParentUnitId, r.Links), ct)).ToOkResult();
}

/// <summary>
/// The body of <see cref="MoveToOrganization"/>. Its name is the OpenAPI schema name: keep it stable.
/// <paramref name="ParentUnitId"/>: the Department unit new Section units go under (needed while a section has neither a
/// unit nor a link). <paramref name="Links"/>: sections to link to units created by hand (from <c>GET /sections/export</c>).
/// </summary>
public sealed record MoveSectionsRequest(Guid? ParentUnitId, IReadOnlyList<SectionUnitLink>? Links = null);
