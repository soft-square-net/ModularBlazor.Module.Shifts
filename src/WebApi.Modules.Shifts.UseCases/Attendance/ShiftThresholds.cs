using Microsoft.Extensions.Options;
using soft.square.net.Organization.Contracts;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.UnitAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.UnitAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Units;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Attendance;

/// <summary>
/// The attendance thresholds that hold for a shift (decision D5). For each value: the nearest unit up the shift's unit
/// path that overrides it (<see cref="UnitAttendance"/>); else, for the check-out grace, the shift's section's
/// override; else the module's setting (<see cref="ShiftOptions"/>).
/// </summary>
public sealed class ShiftThresholds(IReadRepository<UnitAttendance> overrides, UnitCache units, IOptions<ShiftOptions> options)
{
    public async Task<AttendanceThresholds> ForAsync(Shift shift, Section? section, CancellationToken ct)
    {
        var fallback = options.Value.Thresholds;
        if (section is not null) fallback = fallback with { CheckoutGrace = section.CheckoutGraceOr(fallback.CheckoutGrace) };
        return await ForUnitAsync(shift.UnitId, fallback, ct);
    }

    /// <summary>The thresholds of a unit (the module's when it has none, or is not cached).</summary>
    public Task<AttendanceThresholds> ForUnitAsync(Guid? unitId, CancellationToken ct) => ForUnitAsync(unitId, options.Value.Thresholds, ct);

    private async Task<AttendanceThresholds> ForUnitAsync(Guid? unitId, AttendanceThresholds fallback, CancellationToken ct)
    {
        if (unitId is not { } id || await units.FindAsync(id, ct) is not { } unit) return fallback.Checked();
        var chain = UnitPaths.Ids(unit.Path).Select(i => Guid.ParseExact(i, "N")).Reverse().ToArray(); // nearest first
        var rows = (await overrides.ListAsync(new UnitAttendanceOfSpec(chain), ct)).ToDictionary(r => r.Id);
        return UnitAttendance.Resolve(chain.Where(rows.ContainsKey).Select(i => rows[i]), fallback).Checked();
    }
}

/// <summary>Attendance thresholds in minutes, as the API shows them.</summary>
public sealed record AttendanceThresholdsDto(int EarlyCheckInMinutes, int LateMinutes, int AbsentMinutes, int OvertimeMinutes, int CheckoutGraceMinutes)
{
    public static AttendanceThresholdsDto From(AttendanceThresholds t) =>
        new((int)t.EarlyCheckIn.TotalMinutes, (int)t.Late.TotalMinutes, (int)t.Absent.TotalMinutes, (int)t.Overtime.TotalMinutes,
            (int)t.CheckoutGrace.TotalMinutes);
}

/// <summary>A unit's own overrides (null: inherited) and the thresholds that hold for it.</summary>
public sealed record UnitAttendanceDto(Guid UnitId, int? EarlyCheckInMinutes, int? LateMinutes, int? AbsentMinutes, int? OvertimeMinutes,
    int? CheckoutGraceMinutes, AttendanceThresholdsDto Effective);

/// <summary>The attendance status names of the API and the events (<see cref="AttendanceStatuses"/>).</summary>
public static class AttendanceNames
{
    public static string Of(AttendanceStatus status) => status switch
    {
        AttendanceStatus.Expected => AttendanceStatuses.Expected,
        AttendanceStatus.Present => AttendanceStatuses.Present,
        AttendanceStatus.Late => AttendanceStatuses.Late,
        AttendanceStatus.Absent => AttendanceStatuses.Absent,
        AttendanceStatus.LeftEarly => AttendanceStatuses.LeftEarly,
        AttendanceStatus.Overtime => AttendanceStatuses.Overtime,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
}
