using soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.CheckOut;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases.Shifts;

public class CheckOutShiftHandlerHandle
{
    private static CheckOutShiftHandler HandlerFor(ShiftsFixture fixture) => new(fixture.Changes, fixture.Caller, fixture.Authority, fixture.Notices);

    [Fact]
    public async Task ForbidsSomeoneWhoIsNeitherTheManagerNorAShiftAdmin()
    {
        var fixture = new ShiftsFixture(ShiftBuilder.Desk);
        var shift = ShiftBuilder.Published(fixture.Clock.Now.AddHours(-1));
        fixture.Stored(shift);

        var result = await HandlerFor(fixture).Handle(new CheckOutShiftCommand(shift.Id), TestContext.Current.CancellationToken);

        result.Status.ShouldBe(ResultStatus.Forbidden);
        shift.CheckedOutAt.ShouldBeNull();
        await fixture.Shifts.DidNotReceiveWithAnyArgs().UpdateAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task RefusesTheManagerBeforeTheShiftRuns()
    {
        var fixture = new ShiftsFixture(ShiftBuilder.Manager);
        var shift = ShiftBuilder.Published(fixture.Clock.Now.AddHours(1));
        fixture.Stored(shift);

        await Should.ThrowAsync<DomainException>(() => HandlerFor(fixture).Handle(new CheckOutShiftCommand(shift.Id), TestContext.Current.CancellationToken).AsTask());
    }

    [Fact]
    public async Task TheManagerHandsOverAndEveryGrantEndsNow()
    {
        var fixture = new ShiftsFixture(ShiftBuilder.Manager);
        var shift = ShiftBuilder.CheckedIn(fixture.Clock.Now.AddHours(-1));
        fixture.Stored(shift);

        var result = await HandlerFor(fixture).Handle(new CheckOutShiftCommand(shift.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.CheckedOutBy.ShouldBe(ShiftBuilder.Manager);
        result.Value.Running.ShouldBeFalse();
        fixture.WrittenGrants(shift).ShouldAllBe(g => g.ValidTo == fixture.Clock.Now);
        fixture.PublishedOf<ShiftCheckedOut>(ShiftsEvents.CheckedOut).Select(e => e.UserId)
            .ShouldBe([ShiftBuilder.Manager, ShiftBuilder.Desk, ShiftBuilder.Editor], ignoreOrder: true);
        var ended = fixture.PublishedOf<ShiftEnded>(ShiftsEvents.Ended).Single();
        (ended.By, ended.At, ended.Shift.EndedAt).ShouldBe((ShiftBuilder.Manager, fixture.Clock.Now, fixture.Clock.Now));
        fixture.Published.Last().Type.ShouldBe(ShiftsEvents.Ended);
    }

    [Fact]
    public async Task AShiftAdminChecksOutAnOverdueShift()
    {
        var fixture = new ShiftsFixture("workflow");
        fixture.ShiftAdmin();
        var shift = ShiftBuilder.Published(fixture.Clock.Now.AddHours(-9));
        fixture.Stored(shift);

        var result = await HandlerFor(fixture).Handle(new CheckOutShiftCommand(shift.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.CheckedOutAt.ShouldBe(fixture.Clock.Now);
        result.Value.EndedBy.ShouldBe("workflow");
    }
}
