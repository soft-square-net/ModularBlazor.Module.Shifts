using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases;

/// <summary>
/// Module settings (section <c>Shifts</c>). Units override the attendance thresholds (<c>PUT /units/{id}/attendance</c>),
/// and sections the timers, for their shifts.
/// </summary>
public sealed class ShiftOptions
{
    /// <summary>How long the manager has to assign an accepted article before the workflow assigns it. Default 5 minutes.</summary>
    public TimeSpan AutoAssignDelay { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// How long after its scheduled end a member who did not check out keeps their grants; then the clock checks them out
    /// and ends the shift. Default 15 minutes.
    /// </summary>
    public TimeSpan CheckoutGrace { get; set; } = AttendanceThresholds.Default.CheckoutGrace;

    /// <summary>How long before the start a member may check in. Default 30 minutes.</summary>
    public TimeSpan EarlyCheckIn { get; set; } = AttendanceThresholds.Default.EarlyCheckIn;

    /// <summary>A check-in after the start plus this is late. Default 10 minutes.</summary>
    public TimeSpan LateAfter { get; set; } = AttendanceThresholds.Default.Late;

    /// <summary>A member not checked in by the start plus this is marked absent. Default 60 minutes.</summary>
    public TimeSpan AbsentAfter { get; set; } = AttendanceThresholds.Default.Absent;

    /// <summary>Still in, or checking out by hand, after the end plus this is overtime. Default 15 minutes.</summary>
    public TimeSpan OvertimeAfter { get; set; } = AttendanceThresholds.Default.Overtime;

    /// <summary>How often the clock (absences, automatic check-outs, ends) runs for each tenant. Default one minute.</summary>
    public TimeSpan ClockInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>The module's thresholds, before any unit's or section's override.</summary>
    public AttendanceThresholds Thresholds => new(EarlyCheckIn, LateAfter, AbsentAfter, OvertimeAfter, CheckoutGrace);
}
