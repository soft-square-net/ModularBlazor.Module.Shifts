using Ardalis.Specification;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.UnitAggregate;
using soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.CheckIn;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.CheckOutMember;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Handover;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases.Attendance;

public class CheckInHandlerHandle
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static CheckInHandler HandlerFor(ShiftsFixture f) => new(f.Changes, f.Caller, f.Authority, f.Thresholds, f.Notices);

    [Fact]
    public async Task AMemberChecksInAndGetsGrantsFromNowToTheEndPlusTheGrace()
    {
        var fixture = new ShiftsFixture(ShiftBuilder.Editor);
        var shift = ShiftBuilder.Published(fixture.Clock.Now.AddMinutes(-15));
        fixture.Stored(shift);

        var result = await HandlerFor(fixture).Handle(new CheckInCommand(shift.Id), Ct);

        result.IsSuccess.ShouldBeTrue();
        var grants = fixture.WrittenGrants(shift);
        grants.Select(g => g.UserId).Distinct().ShouldBe([ShiftBuilder.Editor]); // nobody else has checked in
        grants.ShouldAllBe(g => g.ValidFrom == fixture.Clock.Now && g.ValidTo == shift.EndsAt + ShiftBuilder.Grace);
        var editor = result.Value.Rota.Single(m => m.UserId == ShiftBuilder.Editor);
        (editor.Status, editor.AccessFrom, editor.AccessEnds).ShouldBe((AttendanceStatuses.Late, fixture.Clock.Now, shift.EndsAt + ShiftBuilder.Grace));

        var e = fixture.PublishedOf<ShiftCheckedIn>(ShiftsEvents.CheckedIn).Single();
        (e.ShiftId, e.UserId, e.By, e.At, e.Status).ShouldBe((shift.Id, ShiftBuilder.Editor, ShiftBuilder.Editor, fixture.Clock.Now, AttendanceStatuses.Late));
        e.RoleKeys.ShouldBe(["editor"]);
        await fixture.Shifts.Received(1).UpdateAsync(shift, Arg.Any<CancellationToken>());
        await fixture.Live.Received(1).ChangedAsync(shift.Id, Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnlyTheLeadOrAManagerChecksSomeoneElseIn()
    {
        var desk = new ShiftsFixture(ShiftBuilder.Desk);
        var shift = ShiftBuilder.Published(desk.Clock.Now);
        desk.Stored(shift);
        (await HandlerFor(desk).Handle(new CheckInCommand(shift.Id, ShiftBuilder.Editor), Ct)).Status.ShouldBe(ResultStatus.Forbidden);

        var lead = new ShiftsFixture(ShiftBuilder.Manager);
        lead.Stored(shift);
        (await HandlerFor(lead).Handle(new CheckInCommand(shift.Id, ShiftBuilder.Editor), Ct)).IsSuccess.ShouldBeTrue();
        shift.Members.Single(m => m.UserId == ShiftBuilder.Editor).CheckedInBy.ShouldBe(ShiftBuilder.Manager);

        var admin = new ShiftsFixture("admin-1");
        admin.ShiftAdmin();
        admin.Stored(shift);
        (await HandlerFor(admin).Handle(new CheckInCommand(shift.Id, ShiftBuilder.Desk), Ct)).IsSuccess.ShouldBeTrue();
        (await HandlerFor(admin).Handle(new CheckInCommand(shift.Id, "stranger"), Ct)).Status.ShouldBe(ResultStatus.NotFound);
    }

    [Fact]
    public async Task CheckInFollowsTheUnitsEarlyThreshold()
    {
        var fixture = new ShiftsFixture(ShiftBuilder.Editor);
        var unit = fixture.Unit(Guid.NewGuid(), "section");
        var row = new UnitAttendance(unit.Id);
        row.Set(earlyCheckIn: 60, null, null, null, null, "admin", fixture.Clock.Now);
        fixture.AttendanceRows.Add(row);
        var shift = ShiftBuilder.Published(fixture.Clock.Now.AddMinutes(45), unit.Id);
        fixture.Stored(shift);

        (await HandlerFor(fixture).Handle(new CheckInCommand(shift.Id), Ct)).IsSuccess.ShouldBeTrue(); // 45 minutes early: allowed by the unit
    }

    [Fact]
    public async Task ACheckOutRevokesAtOnce()
    {
        // The host's grant store evaluates windows: after the check-out the user holds nothing from the shift now, so the
        // live hub (which re-checks the user's subscriptions on every grant write) drops their topics.
        var fixture = new ShiftsFixture(ShiftBuilder.Editor);
        var store = new List<(string Source, NewGrant Grant)>();
        fixture.Grants.ReplaceSourceAsync(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<NewGrant>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                store.RemoveAll(g => g.Source == call.ArgAt<string>(0));
                store.AddRange(call.ArgAt<IReadOnlyCollection<NewGrant>>(1).Select(g => (call.ArgAt<string>(0), g)));
                return Task.CompletedTask;
            });
        bool HoldsNow(string user) => store.Any(g => g.Grant.UserId == user && g.Grant.ValidFrom <= fixture.Clock.Now && fixture.Clock.Now < g.Grant.ValidTo);
        var shift = ShiftBuilder.Published(fixture.Clock.Now.AddMinutes(-5));
        fixture.Stored(shift);

        await HandlerFor(fixture).Handle(new CheckInCommand(shift.Id), Ct);
        fixture.Clock.Advance(TimeSpan.FromMinutes(1));
        HoldsNow(ShiftBuilder.Editor).ShouldBeTrue();

        await new CheckOutMemberHandler(fixture.Changes, fixture.Caller, fixture.Authority, fixture.Notices)
            .Handle(new CheckOutMemberCommand(shift.Id, ShiftBuilder.Editor), Ct);

        HoldsNow(ShiftBuilder.Editor).ShouldBeFalse();
        await fixture.Grants.Received(2).ReplaceSourceAsync(shift.GrantSource, Arg.Any<IReadOnlyCollection<NewGrant>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TheLeadWritesTheHandover()
    {
        var fixture = new ShiftsFixture(ShiftBuilder.Manager);
        var shift = ShiftBuilder.Published(fixture.Clock.Now.AddHours(-7));
        fixture.Stored(shift);

        var result = await new WriteHandoverHandler(fixture.Changes, fixture.Caller, fixture.Notices)
            .Handle(new WriteHandoverCommand(shift.Id, "Story 12 waits for legal."), Ct);

        result.Value.HandoverNote.ShouldBe("Story 12 waits for legal.");
        var e = fixture.PublishedOf<ShiftHandover>(ShiftsEvents.Handover).Single();
        (e.ShiftId, e.Note, e.By, e.At).ShouldBe((shift.Id, "Story 12 waits for legal.", ShiftBuilder.Manager, fixture.Clock.Now));

        var desk = new ShiftsFixture(ShiftBuilder.Desk);
        desk.Stored(shift);
        (await new WriteHandoverHandler(desk.Changes, desk.Caller, desk.Notices).Handle(new WriteHandoverCommand(shift.Id, "x"), Ct))
            .Status.ShouldBe(ResultStatus.Forbidden);
    }
}
