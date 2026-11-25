using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Sections;

/// <summary>
/// The body of <see cref="Add"/> and <see cref="Update"/>. Its name is the OpenAPI schema name: keep it stable.
/// <paramref name="Kind"/>: Category (the default on add) or Department (no categories); left out on update, the section
/// keeps its kind.
/// </summary>
public sealed record SectionRequest(string Name, IReadOnlyList<string> Categories, int? AutoAssignDelayMinutes, int? CheckoutGraceMinutes,
    SectionKind? Kind = null);
