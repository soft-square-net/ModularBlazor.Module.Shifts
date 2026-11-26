using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.ShiftAggregate;

/// <summary>Shifts for tests: an eight-hour shift of section 1 with a manager (the lead), a desk and an editor.</summary>
public static class ShiftBuilder
{
    public const string Manager = "manager-1";
    public const string Desk = "desk-1";
    public const string Editor = "editor-1";

    /// <summary>The default check-out grace: grants of a member still in end this long after the shift.</summary>
    public static readonly TimeSpan Grace = AttendanceThresholds.Default.CheckoutGrace;

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

    /// <summary>Published, and everyone checked in at the start: each member's grants run from the start to the end plus the grace.</summary>
    public static Shift CheckedIn(DateTimeOffset startsAt, Guid? unitId = null)
    {
        var shift = Published(startsAt, unitId);
        CheckEveryoneIn(shift, startsAt);
        return shift;
    }

    /// <summary>Every member of a published shift checks in at <paramref name="at"/>.</summary>
    public static void CheckEveryoneIn(Shift shift, DateTimeOffset at)
    {
        foreach (var user in shift.Members.Select(m => m.UserId).Distinct()) shift.CheckIn(user, user, at, AttendanceThresholds.Default);
    }
}
