namespace soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate.Specifications;

/// <summary>Shifts that overlap [<c>from</c>, <c>to</c>), optionally of one section, with their rota, by start.</summary>
public sealed class ShiftsInWindowSpec : Specification<Shift>
{
    public ShiftsInWindowSpec(DateTimeOffset from, DateTimeOffset? to = null, int? sectionId = null)
    {
        Query.Where(s => s.EndsAt > from);
        if (to is { } end) Query.Where(s => s.StartsAt < end);
        if (sectionId is { } section) Query.Where(s => s.SectionId == section);
        Query.Include(s => s.Members).OrderBy(s => s.StartsAt);
    }
}
