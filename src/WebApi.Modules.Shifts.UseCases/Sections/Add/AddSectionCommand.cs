using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Sections.Add;

/// <summary>
/// Add a section: its kind (default <see cref="SectionKind.Category"/>), the categories it covers (none for a
/// department) and optional overrides of the module's timers (minutes).
/// </summary>
public sealed record AddSectionCommand(string Name, IReadOnlyList<string> Categories, int? AutoAssignDelayMinutes, int? CheckoutGraceMinutes,
    SectionKind? Kind = null) : ICommand<Result<SectionDto>>;
