namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Sections.Update;

/// <summary>
/// Change a section. Changing its categories rewrites the grants of the section's published shifts that have not ended.
/// </summary>
public sealed record UpdateSectionCommand(int Id, string Name, IReadOnlyList<string> Categories, int? AutoAssignDelayMinutes, int? CheckoutGraceMinutes)
    : ICommand<Result<SectionDto>>;
