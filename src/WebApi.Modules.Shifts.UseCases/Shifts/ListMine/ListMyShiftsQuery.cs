namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.ListMine;

/// <summary>The caller's shifts that have not ended (running and upcoming, next 7 days).</summary>
public sealed record ListMyShiftsQuery : IQuery<Result<IReadOnlyList<ShiftDto>>>;
