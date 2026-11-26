namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.CheckOut;

/// <summary>
/// Handover: the whole shift checks out and every member's grants end now. The shift's lead while it runs, or a
/// manager of its unit (<c>shifts.manage</c> on the unit's scopes; the workflow after the grace period).
/// </summary>
public sealed record CheckOutShiftCommand(int Id) : ICommand<Result<ShiftDto>>;
