namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Handover;

/// <summary>
/// The shift's lead writes (or rewrites) the handover note, once the shift has started, also after it ended. Each
/// write is published as <c>shifts.handover.v1</c>.
/// </summary>
public sealed record WriteHandoverCommand(int Id, string Note) : ICommand<Result<ShiftDto>>;
