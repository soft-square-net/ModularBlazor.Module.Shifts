using Microsoft.Extensions.Logging;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Attendance;

/// <summary>What one tick of the clock did in one tenant.</summary>
/// <param name="Absent">Members it marked absent.</param>
/// <param name="CheckedOut">Members it checked out automatically.</param>
/// <param name="Ended">Shifts it ended.</param>
public sealed record ClockTick(int Absent, int CheckedOut, int Ended)
{
    public bool Changed => Absent + CheckedOut + Ended > 0;
}

/// <summary>
/// One tick of the module's clock in the current tenant (the hosted <c>ShiftClock</c> runs it every minute under the
/// tenant's database try-lock). For every published shift that has started and not ended:
/// <list type="number">
/// <item>members not checked in by the start plus the absent threshold are marked absent;</item>
/// <item>at the end plus the check-out grace, everyone still in is checked out automatically at that time
/// (<c>shifts.checked-out.v1</c>, Automatic) and the shift ends (<c>shifts.ended.v1</c>, by
/// <see cref="ShiftsEvents.ClockActor"/>).</item>
/// </list>
/// Each shift is saved on its own, with its outbox rows; then its grants are rewritten and the live nudge sent. It is
/// idempotent: every transition happens once (a marked absence, a check-out, an end are never redone), so a second tick,
/// or a tick that overlaps a user's check-out, changes nothing more.
/// </summary>
public sealed class ShiftClockRun(
    IRepository<Shift> shifts, IReadRepository<Section> sections, ShiftThresholds thresholds, ShiftGrants grants, ShiftNotices notices,
    ShiftCaller caller, ILogger<ShiftClockRun> log)
{
    public async Task<ClockTick> TickAsync(CancellationToken ct)
    {
        var now = caller.Now;
        int absent = 0, checkedOut = 0, ended = 0;
        foreach (var shift in await shifts.ListAsync(new ClockShiftsSpec(now), ct))
        {
            var section = shift.SectionId is { } id ? await sections.FirstOrDefaultAsync(new SectionByIdSpec(id), ct) : null;
            var t = await thresholds.ForAsync(shift, section, ct);
            var marked = shift.MarkAbsences(now, t);
            var outs = shift.EndOnTheClock(ShiftsEvents.ClockActor, now, t);
            if (marked.Count == 0 && outs is null) continue;

            if (outs is not null) await notices.PublishEndAsync(shift, section, outs, ShiftsEvents.ClockActor, automatic: true, ct);
            await shifts.UpdateAsync(shift, ct);
            if (outs is not null) await grants.WriteAsync(shift, section, ct);
            await notices.NudgeAsync(shift, ct);

            absent += marked.Count;
            checkedOut += outs?.Count ?? 0;
            ended += outs is null ? 0 : 1;
            if (outs is not null) log.LogInformation("Shift {Shift} ended on the clock; {Count} member(s) checked out automatically.", shift.Id, outs.Count);
        }
        return new ClockTick(absent, checkedOut, ended);
    }

    /// <summary>
    /// Writes the grants of every published shift that has not ended again (idempotent). The clock does it once per
    /// tenant after the module starts, so the check-in rule reaches the shifts published before it: members who have not
    /// checked in lose the rights an older version wrote at publish.
    /// </summary>
    public async Task<int> RewriteGrantsAsync(CancellationToken ct)
    {
        var list = await shifts.ListAsync(new PublishedNotEndedShiftsSpec(), ct);
        foreach (var shift in list)
        {
            var section = shift.SectionId is { } id ? await sections.FirstOrDefaultAsync(new SectionByIdSpec(id), ct) : null;
            await grants.WriteAsync(shift, section, ct);
        }
        return list.Count;
    }
}
