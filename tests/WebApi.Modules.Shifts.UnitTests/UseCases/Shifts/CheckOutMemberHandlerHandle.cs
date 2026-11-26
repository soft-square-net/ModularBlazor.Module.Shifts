using soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.CheckOutMember;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases.Shifts;

public class CheckOutMemberHandlerHandle
{
    [Fact]
    public async Task AMemberChecksThemselvesOutAndOnlyTheirGrantsEnd()
    {
        var fixture = new ShiftsFixture(ShiftBuilder.Editor);
        var shift = ShiftBuilder.CheckedIn(fixture.Clock.Now.AddHours(-1));
        fixture.Stored(shift);

        var result = await new CheckOutMemberHandler(fixture.Changes, fixture.Caller, fixture.Authority, fixture.Notices)
            .Handle(new CheckOutMemberCommand(shift.Id, ShiftBuilder.Editor), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var grants = fixture.WrittenGrants(shift);
        grants.Where(g => g.UserId == ShiftBuilder.Editor).ShouldAllBe(g => g.ValidTo == fixture.Clock.Now);
        grants.Where(g => g.UserId != ShiftBuilder.Editor).ShouldAllBe(g => g.ValidTo == shift.EndsAt + ShiftBuilder.Grace);
        var e = fixture.PublishedOf<ShiftCheckedOut>(ShiftsEvents.CheckedOut).Single();
        (e.UserId, e.At, e.By, e.Automatic, e.Status).ShouldBe((ShiftBuilder.Editor, fixture.Clock.Now, ShiftBuilder.Editor, false, AttendanceStatuses.LeftEarly));
        await fixture.Live.Received(1).ChangedAsync(shift.Id, Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AMemberCannotCheckOutSomeoneElse()
    {
        var fixture = new ShiftsFixture(ShiftBuilder.Editor);
        var shift = ShiftBuilder.Published(fixture.Clock.Now.AddHours(-1));
        fixture.Stored(shift);

        var result = await new CheckOutMemberHandler(fixture.Changes, fixture.Caller, fixture.Authority, fixture.Notices)
            .Handle(new CheckOutMemberCommand(shift.Id, ShiftBuilder.Desk), TestContext.Current.CancellationToken);

        result.Status.ShouldBe(ResultStatus.Forbidden);
    }
}
