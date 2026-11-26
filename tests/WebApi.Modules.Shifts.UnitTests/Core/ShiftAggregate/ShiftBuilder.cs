using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.ShiftAggregate;

/// <summary>Shifts for tests: an eight-hour shift of section 1 with a manager (the lead), a desk and an editor.</summary>
public static class ShiftBuilder
{
    public const string Manager = "manager-1";
    public const string Desk = "desk-1";
    public const string Editor = "editor-1";

    public static RotaAssignment Lead(string userId) => new(userId, "manager", true);

    public static RotaAssignment As(string userId, string roleKey) => new(userId, roleKey, false);

    public static Shift Planned(DateTimeOffset startsAt, Guid? unitId = null) =>
        Shift.Create(1, startsAt, startsAt.AddHours(8), [Lead(Manager), As(Desk, "desk"), As(Editor, "editor")], unitId);

    public static Shift Published(DateTimeOffset startsAt, Guid? unitId = null)
    {
        var shift = Planned(startsAt, unitId);
        shift.Publish();
        return shift;
    }
}
