namespace soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate.Specifications;

/// <summary>
/// The shifts the clock looks at: published, not ended, already started (absences and the end only come after the
/// start), with their rota, by start.
/// </summary>
public sealed class ClockShiftsSpec : Specification<Shift>
{
    public ClockShiftsSpec(DateTimeOffset now) =>
        Query.Where(s => s.Published && s.CheckedOutAt == null && s.StartsAt <= now)
            .Include(s => s.Members)
            .OrderBy(s => s.StartsAt);
}

/// <summary>Published shifts that have not ended, with their rota (to write their grants again).</summary>
public sealed class PublishedNotEndedShiftsSpec : Specification<Shift>
{
    public PublishedNotEndedShiftsSpec() =>
        Query.Where(s => s.Published && s.CheckedOutAt == null).Include(s => s.Members);
}

/// <summary>Shifts that ended at or after <c>since</c>, in the order they ended (then by id).</summary>
public sealed class EndedShiftsSpec : Specification<Shift>
{
    public EndedShiftsSpec(DateTimeOffset since, int take) =>
        Query.Where(s => s.EndedAt != null && s.EndedAt >= since)
            .OrderBy(s => s.EndedAt).ThenBy(s => s.Id)
            .Take(take);
}
