using soft.square.net.Organization.Contracts;
using soft.square.net.WebApi.Contracts.Messaging;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Attendance;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Units;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;

/// <summary>
/// Live nudges about a shift, sent after its change is committed (best effort; the implementation is the
/// Infrastructure's, over the platform's <c>ILiveNotifier</c>).
/// </summary>
public interface IShiftLive
{
    /// <summary>
    /// <see cref="ShiftsLive.ChangedMessage"/> with <see cref="ShiftChanged"/> on <c>shifts:shift:{id}</c> and, when the
    /// shift's unit is known, on the unit's topic and every ancestor's (<paramref name="unitPath"/>).
    /// </summary>
    Task ChangedAsync(int shiftId, string? unitPath, CancellationToken ct);
}

/// <summary>
/// The shifts.* integration events of a change (<see cref="ShiftsEvents"/>), written to the outbox before the change's
/// save so they go out with it, and the live nudge after it. The envelope carries the shift's unit path and org when
/// the unit is cached.
/// </summary>
public sealed class ShiftNotices(IIntegrationEvents events, IShiftLive live, UnitCache units, ShiftThresholds thresholds, ShiftCaller caller)
{
    /// <summary>The shift as the events carry it, with everyone's attendance now.</summary>
    public async Task<ShiftSnapshot> SnapshotAsync(Shift shift, Section? section, CancellationToken ct)
    {
        var t = await thresholds.ForAsync(shift, section, ct);
        var path = shift.UnitId is { } id ? (await units.FindAsync(id, ct))?.Path : null;
        var now = caller.Now;
        return new ShiftSnapshot(shift.Id, shift.UnitId, path, shift.SectionId, shift.StartsAt, shift.EndsAt, shift.Published, shift.EndedAt ?? shift.CheckedOutAt,
            shift.HandoverNote,
            [.. shift.Members.Select(m => new ShiftMemberSnapshot(m.UserId, m.RoleKey, m.IsLead, m.CheckedInAt, m.CheckedOutAt, m.AbsentAt,
                AttendanceNames.Of(m.StatusOn(shift, t, now))))]);
    }

    /// <summary>One person's attendance on the shift now (all their roles share it).</summary>
    public async Task<string> StatusOfAsync(Shift shift, Section? section, string userId, CancellationToken ct)
    {
        var t = await thresholds.ForAsync(shift, section, ct);
        return AttendanceNames.Of(shift.Members.First(m => m.UserId == userId).StatusOn(shift, t, caller.Now));
    }

    /// <summary>Adds the event to the outbox (sent with the next save of the module's context).</summary>
    public async Task PublishAsync(Shift shift, string type, object data, CancellationToken ct)
    {
        var unit = shift.UnitId is { } id ? await units.FindAsync(id, ct) : null;
        events.Publish(type, data, ShiftsEvents.ShiftSubject(shift.Id), unit?.Path, unit is null ? null : UnitPaths.Id(unit.OrgId));
    }

    /// <summary>
    /// The rota changes of a published shift, from <paramref name="before"/> to now: <c>member-added</c> and
    /// <c>member-removed</c> per person and role. True when there was any.
    /// </summary>
    public async Task<bool> PublishRotaChangesAsync(Shift shift, IReadOnlyList<RotaAssignment> before, CancellationToken ct)
    {
        var after = shift.Rota;
        var removed = before.Where(b => !after.Contains(b)).ToArray();
        var added = after.Where(a => !before.Contains(a)).ToArray();
        foreach (var r in removed)
            await PublishAsync(shift, ShiftsEvents.MemberRemoved, new ShiftMemberRemoved(shift.Id, shift.UnitId, r.UserId, r.RoleKey, r.IsLead), ct);
        foreach (var a in added)
            await PublishAsync(shift, ShiftsEvents.MemberAdded, new ShiftMemberAdded(shift.Id, shift.UnitId, a.UserId, a.RoleKey, a.IsLead), ct);
        return removed.Length + added.Length > 0;
    }

    /// <summary><c>checked-out</c> for each user, then <c>ended</c>: what a check-out of the whole shift, or the clock's end, reports.</summary>
    public async Task PublishEndAsync(Shift shift, Section? section, IReadOnlyList<string> checkedOut, string by, bool automatic, CancellationToken ct)
    {
        foreach (var userId in checkedOut)
        {
            var at = shift.Members.First(m => m.UserId == userId).CheckedOutAt!.Value;
            await PublishAsync(shift, ShiftsEvents.CheckedOut,
                new ShiftCheckedOut(shift.Id, shift.UnitId, userId, at, by, automatic, await StatusOfAsync(shift, section, userId, ct)), ct);
        }
        await PublishAsync(shift, ShiftsEvents.Ended, new ShiftEnded(await SnapshotAsync(shift, section, ct), by, shift.EndedAt!.Value), ct);
    }

    /// <summary>The live nudge, after the commit. Never throws.</summary>
    public async Task NudgeAsync(Shift shift, CancellationToken ct)
    {
        var path = shift.UnitId is { } id ? (await units.FindAsync(id, ct))?.Path : null;
        await live.ChangedAsync(shift.Id, path, ct);
    }
}
