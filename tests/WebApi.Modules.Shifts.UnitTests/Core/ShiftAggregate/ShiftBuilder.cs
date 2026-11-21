using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.ShiftAggregate;

/// <summary>Shifts for tests: an eight-hour shift of section 1 with a manager, a desk and an editor.</summary>
public static class ShiftBuilder
{
    public const string Manager = "manager-1";
    public const string Desk = "desk-1";
    public const string Editor = "editor-1";

    public static Shift Planned(DateTimeOffset startsAt) =>
        Shift.Create(1, startsAt, startsAt.AddHours(8), [(Manager, ShiftRole.Manager), (Desk, ShiftRole.Desk), (Editor, ShiftRole.Editor)]);

    public static Shift Published(DateTimeOffset startsAt)
    {
        var shift = Planned(startsAt);
        shift.Publish();
        return shift;
    }
}
