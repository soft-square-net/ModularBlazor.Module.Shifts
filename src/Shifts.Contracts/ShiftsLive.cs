namespace soft.square.net.Shifts.Contracts;

/// <summary>
/// The module's live messages. After each committed change of a shift the module publishes <see cref="ChangedMessage"/>
/// with the payload <see cref="ShiftChanged"/> (no sequence) on two topics:
/// <list type="bullet">
/// <item><c>shifts:shift:{id}</c> (<see cref="Topic"/>): the shift's own topic. A subscriber needs <c>shifts.view</c>
/// for <c>shift:{id}</c>, or for the shift's unit or one of its ancestors (<c>unit:{id}</c>), or tenant-wide.</item>
/// <item><c>unit:{id}</c> for the shift's unit and every ancestor (the platform's unit topics, authorized by the
/// Organization module's permission).</item>
/// </list>
/// It is a nudge: the subscriber re-reads the shift.
/// </summary>
public static class ShiftsLive
{
    public const string ModuleId = "shifts";
    public const string Kind = "shift";

    /// <summary>The message type: something of the shift changed (its rota, its attendance, its handover, its end).</summary>
    public const string ChangedMessage = "shift.changed";

    public static string Topic(int shiftId) => $"{ModuleId}:{Kind}:{shiftId}";
}

/// <summary>The payload of <see cref="ShiftsLive.ChangedMessage"/>.</summary>
public sealed record ShiftChanged(int ShiftId);

/// <summary>The attendance status of a rota member, as <see cref="ShiftMemberSnapshot.Status"/> names it.</summary>
public static class AttendanceStatuses
{
    /// <summary>Not checked in yet, and not late enough to be absent.</summary>
    public const string Expected = "expected";
    /// <summary>Checked in on time (at most the late threshold after the start).</summary>
    public const string Present = "present";
    /// <summary>Checked in after the start plus the late threshold.</summary>
    public const string Late = "late";
    /// <summary>Not checked in by the start plus the absent threshold (marked by the clock), and never since.</summary>
    public const string Absent = "absent";
    /// <summary>Checked out before the shift's end.</summary>
    public const string LeftEarly = "left-early";
    /// <summary>Still checked in, or checked out, after the end plus the overtime threshold.</summary>
    public const string Overtime = "overtime";

    public static IReadOnlyList<string> All { get; } = [Expected, Present, Late, Absent, LeftEarly, Overtime];
}
