namespace soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;

/// <summary>A rota member's attendance, derived from their check-in, check-out and absence (never stored).</summary>
public enum AttendanceStatus
{
    /// <summary>Not checked in yet, and not late enough to be absent.</summary>
    Expected,
    /// <summary>Checked in on time: at most <see cref="AttendanceThresholds.Late"/> after the start.</summary>
    Present,
    /// <summary>Checked in after the start plus <see cref="AttendanceThresholds.Late"/>.</summary>
    Late,
    /// <summary>Never checked in, and marked absent (the start plus <see cref="AttendanceThresholds.Absent"/> passed).</summary>
    Absent,
    /// <summary>Checked out (themselves or by a manager) before the shift's end.</summary>
    LeftEarly,
    /// <summary>Still checked in, or checked out by hand, later than the end plus <see cref="AttendanceThresholds.Overtime"/>.</summary>
    Overtime
}

/// <summary>
/// The attendance thresholds of a shift (decision D5): the module's settings, overridden per unit (the nearest unit up
/// the path that sets one), or per section for a shift that has no unit.
/// </summary>
/// <param name="EarlyCheckIn">How long before the start a member may check in. Default 30 minutes.</param>
/// <param name="Late">A check-in after the start plus this is late. Default 10 minutes.</param>
/// <param name="Absent">A member not checked in by the start plus this is marked absent. Default 60 minutes.</param>
/// <param name="Overtime">Still in (or checking out by hand) after the end plus this is overtime. Default 15 minutes.</param>
/// <param name="CheckoutGrace">
/// A member who does not check out keeps their grants until the end plus this, when the clock checks them out and ends
/// the shift. Default 15 minutes.
/// </param>
public sealed record AttendanceThresholds(TimeSpan EarlyCheckIn, TimeSpan Late, TimeSpan Absent, TimeSpan Overtime, TimeSpan CheckoutGrace)
{
    public static AttendanceThresholds Default { get; } =
        new(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(60), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15));

    /// <summary>Thresholds are never negative.</summary>
    public AttendanceThresholds Checked()
    {
        if (EarlyCheckIn < TimeSpan.Zero || Late < TimeSpan.Zero || Absent < TimeSpan.Zero || Overtime < TimeSpan.Zero || CheckoutGrace < TimeSpan.Zero)
            throw new DomainException("Attendance thresholds cannot be negative.");
        return this;
    }
}

/// <summary>A grant window: from a check-in to a check-out (or the end plus the grace).</summary>
public readonly record struct AccessWindow(DateTimeOffset From, DateTimeOffset To);
