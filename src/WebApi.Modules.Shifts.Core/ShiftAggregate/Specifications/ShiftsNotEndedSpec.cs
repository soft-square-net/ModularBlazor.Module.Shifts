namespace soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate.Specifications;

/// <summary>Shifts that have not ended (not checked out, end still ahead) with a member in role <c>roleKey</c>, with their rota.</summary>
public sealed class ShiftsNotEndedWithRoleSpec : Specification<Shift>
{
    public ShiftsNotEndedWithRoleSpec(string roleKey, DateTimeOffset now) =>
        Query.Where(s => s.CheckedOutAt == null && s.EndsAt > now && s.Members.Any(m => m.RoleKey == roleKey))
            .Include(s => s.Members);
}
