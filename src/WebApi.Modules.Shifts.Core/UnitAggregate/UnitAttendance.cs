using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.Core.UnitAggregate;

/// <summary>
/// A unit's overrides of the attendance thresholds (table <c>unit_attendance</c>, keyed by the unit's id). Each value
/// is optional: a threshold a unit does not set comes from the nearest unit up its path that does, else from the
/// module's settings (decision D5). Shifts owns this data; the unit itself is Organization's.
/// </summary>
public sealed class UnitAttendance : AggregateRoot<Guid>, ITenantOwned
{
    private UnitAttendance() { } // EF Core

    public UnitAttendance(Guid unitId) => Id = unitId;

    public string TenantId { get; set; } = "";
    public int? EarlyCheckInMinutes { get; private set; }
    public int? LateMinutes { get; private set; }
    public int? AbsentMinutes { get; private set; }
    public int? OvertimeMinutes { get; private set; }
    public int? CheckoutGraceMinutes { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public string UpdatedBy { get; private set; } = "";

    public bool IsEmpty => EarlyCheckInMinutes is null && LateMinutes is null && AbsentMinutes is null && OvertimeMinutes is null && CheckoutGraceMinutes is null;

    /// <summary>Sets every override at once (null: inherit). At most a day each.</summary>
    public void Set(int? earlyCheckIn, int? late, int? absent, int? overtime, int? checkoutGrace, string by, DateTimeOffset now)
    {
        foreach (var value in new[] { earlyCheckIn, late, absent, overtime, checkoutGrace })
            if (value is < 0 or > 24 * 60) throw new DomainException("An attendance threshold is between 0 and 1440 minutes.");
        EarlyCheckInMinutes = earlyCheckIn;
        LateMinutes = late;
        AbsentMinutes = absent;
        OvertimeMinutes = overtime;
        CheckoutGraceMinutes = checkoutGrace;
        UpdatedAt = now;
        UpdatedBy = by;
    }

    /// <summary>
    /// The thresholds for a unit whose overrides, from the unit itself up to the root, are <paramref name="nearestFirst"/>:
    /// for each value the first that sets it, else <paramref name="fallback"/>'s.
    /// </summary>
    public static AttendanceThresholds Resolve(IEnumerable<UnitAttendance> nearestFirst, AttendanceThresholds fallback)
    {
        var chain = nearestFirst.ToArray();
        TimeSpan Pick(Func<UnitAttendance, int?> value, TimeSpan otherwise) =>
            chain.Select(value).FirstOrDefault(v => v is not null) is { } minutes ? TimeSpan.FromMinutes(minutes) : otherwise;
        return new AttendanceThresholds(
            Pick(u => u.EarlyCheckInMinutes, fallback.EarlyCheckIn),
            Pick(u => u.LateMinutes, fallback.Late),
            Pick(u => u.AbsentMinutes, fallback.Absent),
            Pick(u => u.OvertimeMinutes, fallback.Overtime),
            Pick(u => u.CheckoutGraceMinutes, fallback.CheckoutGrace));
    }
}
