namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.CheckOut;

/// <summary>
/// Handover: the whole shift checks out and every member's grants end now. The shift's manager while it runs, or a
/// shift admin (the workflow after the grace period).
/// </summary>
public sealed record CheckOutShiftCommand(int Id) : ICommand<Result<ShiftDto>>;
