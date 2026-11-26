namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.CheckOut;

/// <summary>
/// Handover: the whole shift checks out, everyone still in is checked out now (their grants end), and the shift ends.
/// The shift's lead once it has started, or a manager of its unit (<c>shifts.manage</c> on the unit's scopes). After
/// the end plus the grace the module's clock does it on its own.
/// </summary>
public sealed record CheckOutShiftCommand(int Id) : ICommand<Result<ShiftDto>>;
