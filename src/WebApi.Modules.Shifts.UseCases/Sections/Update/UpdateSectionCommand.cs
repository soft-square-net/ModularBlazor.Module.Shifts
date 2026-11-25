using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Sections.Update;

/// <summary>
/// Change a section. Changing its categories or its kind rewrites the grants of the section's published shifts that
/// have not ended. A null <paramref name="Kind"/> keeps the section's kind.
/// </summary>
public sealed record UpdateSectionCommand(int Id, string Name, IReadOnlyList<string> Categories, int? AutoAssignDelayMinutes, int? CheckoutGraceMinutes,
    SectionKind? Kind = null) : ICommand<Result<SectionDto>>;
