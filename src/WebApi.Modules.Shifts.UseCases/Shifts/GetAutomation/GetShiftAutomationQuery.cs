namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.GetAutomation;

/// <summary>The shift's clock and its section's assignment and check-out timers, for the workflow service.</summary>
public sealed record GetShiftAutomationQuery(int Id) : IQuery<Result<ShiftAutomationInfo>>;
