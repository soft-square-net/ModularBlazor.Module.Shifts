namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.ListOverdue;

/// <summary>Published shifts past their end that nobody checked out (the workflow checks them out after the grace period).</summary>
public sealed record ListOverdueShiftsQuery : IQuery<Result<IReadOnlyList<ShiftDto>>>;
