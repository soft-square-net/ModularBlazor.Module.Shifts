namespace soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;

/// <summary>
/// One person in one role on a shift's rota. <see cref="RoleKey"/> names a role definition (resolved for the shift's
/// unit); <see cref="IsLead"/> is the definition's lead flag when the person was put on the rota.
/// <para>Attendance: a person checks in once per shift (all of their roles at once), and their grants run from
/// <see cref="CheckedInAt"/> to <see cref="CheckedOutAt"/>, or to the shift's end plus the check-out grace while they
/// are still in. Nobody who has not checked in has any right from the shift.</para>
/// </summary>
public sealed class ShiftMember : EntityBase<int>
{
    private ShiftMember() { } // EF Core

    public ShiftMember(string userId, string roleKey, bool isLead)
    {
        UserId = userId;
        RoleKey = roleKey;
        IsLead = isLead;
    }

    public string UserId { get; private set; } = "";
    public string RoleKey { get; private set; } = "";
    public bool IsLead { get; private set; }

    /// <summary>When the member checked in (their grants start then).</summary>
    public DateTimeOffset? CheckedInAt { get; private set; }
    /// <summary>Who checked them in: themselves, or a manager or the lead for them.</summary>
    public string? CheckedInBy { get; private set; }
    /// <summary>When the member's grants ended: their own or a manager's check-out, the shift's, or the clock's.</summary>
    public DateTimeOffset? CheckedOutAt { get; private set; }
    public string? CheckedOutBy { get; private set; }
    /// <summary>The clock checked them out at the end plus the grace (they never checked out).</summary>
    public bool CheckedOutAutomatically { get; private set; }
    /// <summary>When the clock marked them absent (not checked in by the start plus the absent threshold).</summary>
    public DateTimeOffset? AbsentAt { get; private set; }

    public bool IsIn => CheckedInAt is not null && CheckedOutAt is null;

    internal void CheckIn(DateTimeOffset at, string by)
    {
        CheckedInAt = at;
        CheckedInBy = by;
    }

    /// <summary>Checks in again after checking out, while the shift runs: the grants continue from the first check-in.</summary>
    internal void Resume()
    {
        CheckedOutAt = null;
        CheckedOutBy = null;
        CheckedOutAutomatically = false;
    }

    internal void CheckOut(DateTimeOffset at, string by, bool automatically = false)
    {
        CheckedOutAt = at;
        CheckedOutBy = by;
        CheckedOutAutomatically = automatically;
    }

    internal void MarkAbsent(DateTimeOffset at) => AbsentAt = at;

    /// <summary>A new role of the same person on the same shift: the same attendance.</summary>
    internal void CopyAttendance(ShiftMember other)
    {
        CheckedInAt = other.CheckedInAt;
        CheckedInBy = other.CheckedInBy;
        CheckedOutAt = other.CheckedOutAt;
        CheckedOutBy = other.CheckedOutBy;
        CheckedOutAutomatically = other.CheckedOutAutomatically;
        AbsentAt = other.AbsentAt;
    }

    /// <summary>The member's attendance on <paramref name="shift"/> at <paramref name="now"/>.</summary>
    public AttendanceStatus StatusOn(Shift shift, AttendanceThresholds thresholds, DateTimeOffset now)
    {
        if (CheckedInAt is not { } checkedIn) return AbsentAt is not null ? AttendanceStatus.Absent : AttendanceStatus.Expected;
        if (CheckedOutAt is { } checkedOut && !CheckedOutAutomatically)
        {
            if (checkedOut < shift.EndsAt) return AttendanceStatus.LeftEarly;
            if (checkedOut > shift.EndsAt + thresholds.Overtime) return AttendanceStatus.Overtime;
        }
        else if (CheckedOutAt is null && now > shift.EndsAt + thresholds.Overtime) return AttendanceStatus.Overtime;
        return checkedIn > shift.StartsAt + thresholds.Late ? AttendanceStatus.Late : AttendanceStatus.Present;
    }

    /// <summary>
    /// The member's grant window: from their check-in to their check-out, or to the shift's end plus the grace while they
    /// are in. Null when they never checked in, or when the window is empty.
    /// </summary>
    public AccessWindow? AccessOn(Shift shift, TimeSpan checkoutGrace)
    {
        if (CheckedInAt is not { } from) return null;
        var to = CheckedOutAt ?? shift.EndsAt + checkoutGrace;
        return to > from ? new AccessWindow(from, to) : null;
    }
}

/// <summary>One entry of a rota: a person, the key of their role, and whether that role is the shift's lead.</summary>
public readonly record struct RotaAssignment(string UserId, string RoleKey, bool IsLead);
