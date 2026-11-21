namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Sections.Add;

/// <summary>Add a section: the categories it covers and optional overrides of the module's timers (minutes).</summary>
public sealed record AddSectionCommand(string Name, IReadOnlyList<string> Categories, int? AutoAssignDelayMinutes, int? CheckoutGraceMinutes)
    : ICommand<Result<SectionDto>>;
