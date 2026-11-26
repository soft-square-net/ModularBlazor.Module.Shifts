using Ardalis.Specification;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.UnitAggregate;
using Microsoft.Extensions.Logging.Abstractions;
using soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Attendance;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases.Attendance;

/// <summary>The clock's transitions on a fake clock: absences, the automatic check-out and the end, each once.</summary>
public class ShiftClockRunTick
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly ShiftsFixture _fixture = new("nobody");
    private readonly Shift _shift;
    private readonly ShiftClockRun _clock;

    public ShiftClockRunTick()
    {
        _shift = ShiftBuilder.Published(_fixture.Clock.Now); // 09:00 to 17:00
        _fixture.Shifts.ListAsync(Arg.Any<ISpecification<Shift>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<ISpecification<Shift>>().IsSatisfiedBy(_shift) ? [_shift] : new List<Shift>());
        _clock = new ShiftClockRun(_fixture.Shifts, _fixture.SectionReader, _fixture.Thresholds, _fixture.ShiftGrants, _fixture.Notices,
            _fixture.Caller, NullLogger<ShiftClockRun>.Instance);
    }

    [Fact]
    public async Task MarksAbsencesAfterTheThresholdOnce()
    {
        _shift.CheckIn(ShiftBuilder.Desk, ShiftBuilder.Desk, _fixture.Clock.Now, AttendanceThresholds.Default);

        _fixture.Clock.Advance(TimeSpan.FromMinutes(59));
        (await _clock.TickAsync(Ct)).Changed.ShouldBeFalse();

        _fixture.Clock.Advance(TimeSpan.FromMinutes(1));
        (await _clock.TickAsync(Ct)).ShouldBe(new ClockTick(2, 0, 0));
        (await _clock.TickAsync(Ct)).Changed.ShouldBeFalse();

        _shift.Members.Where(m => m.UserId != ShiftBuilder.Desk).ShouldAllBe(m => m.AbsentAt == TestClock.Start.AddHours(1));
        await _fixture.Shifts.Received(1).UpdateAsync(_shift, Arg.Any<CancellationToken>());
        await _fixture.Live.Received(1).ChangedAsync(_shift.Id, Arg.Any<string?>(), Arg.Any<CancellationToken>());
        _fixture.Published.ShouldBeEmpty(); // an absence has no event of its own: it is in the ended snapshot
    }

    [Fact]
    public async Task ChecksEveryoneOutAndEndsTheShiftAtTheEndPlusTheGraceOnce()
    {
        _shift.CheckIn(ShiftBuilder.Desk, ShiftBuilder.Desk, _fixture.Clock.Now, AttendanceThresholds.Default);
        _shift.CheckIn(ShiftBuilder.Editor, ShiftBuilder.Editor, _fixture.Clock.Now.AddMinutes(20), AttendanceThresholds.Default);

        _fixture.Clock.Advance(TimeSpan.FromHours(8) + TimeSpan.FromMinutes(14));
        (await _clock.TickAsync(Ct)).ShouldBe(new ClockTick(1, 0, 0)); // the manager never came
        _shift.Ended.ShouldBeFalse();

        _fixture.Clock.Advance(TimeSpan.FromMinutes(2)); // 17:16, one minute after end + grace
        (await _clock.TickAsync(Ct)).ShouldBe(new ClockTick(0, 2, 1));
        (await _clock.TickAsync(Ct)).Changed.ShouldBeFalse();

        var end = _shift.EndsAt + ShiftBuilder.Grace;
        (_shift.CheckedOutAt, _shift.EndedAt, _shift.EndedBy).ShouldBe((end, _fixture.Clock.Now, ShiftsEvents.ClockActor));
        _fixture.WrittenGrants(_shift).ShouldAllBe(g => g.ValidTo == end);

        var outs = _fixture.PublishedOf<ShiftCheckedOut>(ShiftsEvents.CheckedOut);
        outs.Select(o => (o.UserId, o.Automatic, o.At, o.By)).ShouldBe(
            [(ShiftBuilder.Desk, true, end, ShiftsEvents.ClockActor), (ShiftBuilder.Editor, true, end, ShiftsEvents.ClockActor)], ignoreOrder: true);
        outs.Single(o => o.UserId == ShiftBuilder.Editor).Status.ShouldBe(AttendanceStatuses.Late);
        var ended = _fixture.PublishedOf<ShiftEnded>(ShiftsEvents.Ended).Single();
        (ended.By, ended.At).ShouldBe((ShiftsEvents.ClockActor, _fixture.Clock.Now));
        ended.Shift.Members.Single(m => m.UserId == ShiftBuilder.Manager).Status.ShouldBe(AttendanceStatuses.Absent);
        _fixture.Published.Count(e => e.Type == ShiftsEvents.Ended).ShouldBe(1);
    }

    [Fact]
    public async Task AShiftCheckedOutByHandIsLeftAlone()
    {
        _fixture.Clock.Advance(TimeSpan.FromHours(4));
        _shift.CheckOut(ShiftBuilder.Manager, _fixture.Clock.Now);

        _fixture.Clock.Advance(TimeSpan.FromHours(5));
        (await _clock.TickAsync(Ct)).Changed.ShouldBeFalse();
        _shift.EndedBy.ShouldBe(ShiftBuilder.Manager);
    }

    [Fact]
    public async Task AUnitsLongerGraceDelaysTheEnd()
    {
        var unit = _fixture.Unit(Guid.NewGuid(), "section");
        var row = new UnitAttendance(unit.Id);
        row.Set(null, null, null, null, checkoutGrace: 60, "admin", _fixture.Clock.Now);
        _fixture.AttendanceRows.Add(row);
        var shift = ShiftBuilder.Published(_fixture.Clock.Now, unit.Id);
        _fixture.Shifts.ListAsync(Arg.Any<ISpecification<Shift>>(), Arg.Any<CancellationToken>()).Returns([shift]);

        _fixture.Clock.Advance(TimeSpan.FromHours(8) + TimeSpan.FromMinutes(30));
        (await _clock.TickAsync(Ct)).Ended.ShouldBe(0);
        _fixture.Clock.Advance(TimeSpan.FromMinutes(30));
        (await _clock.TickAsync(Ct)).Ended.ShouldBe(1);
    }

    [Fact]
    public async Task RewritingTheGrantsDropsMembersWhoHaveNotCheckedIn()
    {
        _fixture.Shifts.ListAsync(Arg.Any<ISpecification<Shift>>(), Arg.Any<CancellationToken>()).Returns([_shift]);

        (await _clock.RewriteGrantsAsync(Ct)).ShouldBe(1);

        _fixture.WrittenGrants(_shift).ShouldBeEmpty();
    }
}
