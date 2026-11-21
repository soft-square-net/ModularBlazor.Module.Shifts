namespace soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate.Specifications;

/// <summary>The shifts of one person that overlap [<c>from</c>, <c>to</c>), with their rota, by start.</summary>
public sealed class MemberShiftsSpec : Specification<Shift>
{
    public MemberShiftsSpec(string userId, DateTimeOffset from, DateTimeOffset to) =>
        Query.Where(s => s.EndsAt > from && s.StartsAt < to && s.Members.Any(m => m.UserId == userId))
            .Include(s => s.Members)
            .OrderBy(s => s.StartsAt);
}
