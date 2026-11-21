namespace soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate.Specifications;

/// <summary>Published shifts past their end that nobody checked out, with their rota, by start.</summary>
public sealed class OverdueShiftsSpec : Specification<Shift>
{
    public OverdueShiftsSpec(DateTimeOffset now) =>
        Query.Where(s => s.Published && s.CheckedOutAt == null && s.EndsAt <= now)
            .Include(s => s.Members)
            .OrderBy(s => s.StartsAt);
}
