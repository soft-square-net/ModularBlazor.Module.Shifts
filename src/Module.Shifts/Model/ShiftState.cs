using soft.square.net.WebApi.Modules.Shifts.Client;

namespace soft.square.net.Module.Shifts.Model;

/// <summary>
/// Where a shift and its members stand, and which buttons the pages show. The API decides who may do what; these only
/// hide the buttons that cannot work. The texts are English: they are the keys of the module's translations.
/// </summary>
public static class ShiftState
{
    public static string Status(ShiftDto shift, DateTimeOffset now) => shift switch
    {
        { CheckedOutAt: not null } => "Checked out",
        { Published: true } when shift.StartsAt <= now && now < shift.EndsAt => "Running",
        { Published: false } when shift.StartsAt <= now && now < shift.EndsAt => "Running, not published",
        { Published: true } when shift.EndsAt <= now => "Overdue",
        { Published: true } => "Published",
        _ when shift.EndsAt <= now => "Ended, not published",
        _ => "Planned"
    };

    public static readonly string[] Statuses =
        ["Planned", "Published", "Running", "Running, not published", "Overdue", "Ended, not published", "Checked out"];

    public static string StatusClass(string status) => status switch
    {
        "Running" => "sh-chip-on",
        "Overdue" or "Running, not published" => "sh-chip-warn",
        _ => ""
    };

    /// <summary>The API's attendance statuses, in the order the board shows them.</summary>
    public static readonly string[] AttendanceStatuses = ["expected", "present", "late", "absent", "left-early", "overtime"];

    public static string AttendanceText(string status) => status switch
    {
        "present" => "Present",
        "late" => "Late",
        "absent" => "Absent",
        "left-early" => "Left early",
        "overtime" => "Overtime",
        _ => "Expected"
    };

    public static string AttendanceClass(string status) => status switch
    {
        "present" or "overtime" => "sh-chip-on",
        "late" or "left-early" or "absent" => "sh-chip-warn",
        _ => ""
    };

    /// <summary>Check-in opens before the start (the early threshold) and closes at the end, on a published shift.</summary>
    public static bool CheckInOpen(ShiftDto shift, DateTimeOffset now) =>
        shift is { Published: true, Ended: false } && shift.CheckInOpensAt <= now && now < shift.EndsAt;

    /// <summary>Not in now: never checked in, or checked out (checking in again is allowed before the end).</summary>
    public static bool IsOut(RotaMemberDto member) => member.CheckedInAt is null || member.CheckedOutAt is not null;

    public static bool IsIn(RotaMemberDto member) => member.CheckedInAt is not null && member.CheckedOutAt is null;

    public static bool CanCheckIn(ShiftDto shift, string? userId, DateTimeOffset now) =>
        CheckInOpen(shift, now) && shift.Rota.Any(m => m.UserId == userId && IsOut(m));

    public static bool CanCheckOut(ShiftDto shift, string? userId) => !shift.Ended && shift.Rota.Any(m => m.UserId == userId && IsIn(m));

    /// <summary>The whole shift can be checked out once a published shift has started, until it ends.</summary>
    public static bool CanCheckOutShift(ShiftDto shift, DateTimeOffset now) =>
        shift is { Published: true, CheckedOutAt: null } && shift.StartsAt <= now;

    /// <summary>People on the rota (a person with two roles counts once), and how many of them are in now.</summary>
    public static (int In, int People) Presence(ShiftDto shift) =>
        (shift.Rota.Where(IsIn).Select(m => m.UserId).Distinct().Count(), shift.Rota.Select(m => m.UserId).Distinct().Count());
}
