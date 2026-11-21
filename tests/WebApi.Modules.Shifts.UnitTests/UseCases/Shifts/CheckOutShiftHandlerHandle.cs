using soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.CheckOut;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases.Shifts;

public class CheckOutShiftHandlerHandle
{
    private static CheckOutShiftHandler HandlerFor(ShiftsFixture fixture) => new(fixture.Changes, fixture.Caller);

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
    public async Task ForbidsTheManagerBeforeTheShiftRuns()
    {
        var fixture = new ShiftsFixture(ShiftBuilder.Manager);
        var shift = ShiftBuilder.Published(fixture.Clock.Now.AddHours(1));
        fixture.Stored(shift);

        var result = await HandlerFor(fixture).Handle(new CheckOutShiftCommand(shift.Id), TestContext.Current.CancellationToken);

        result.Status.ShouldBe(ResultStatus.Forbidden);
    }

    [Fact]
    public async Task TheManagerHandsOverAndEveryGrantEndsNow()
    {
        var fixture = new ShiftsFixture(ShiftBuilder.Manager);
        var shift = ShiftBuilder.Published(fixture.Clock.Now.AddHours(-1));
        fixture.Stored(shift);

        var result = await HandlerFor(fixture).Handle(new CheckOutShiftCommand(shift.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.CheckedOutBy.ShouldBe(ShiftBuilder.Manager);
        result.Value.Running.ShouldBeFalse();
        fixture.WrittenGrants(shift).ShouldAllBe(g => g.ValidTo == fixture.Clock.Now);
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
        result.Value.CheckedOutAt.ShouldBe(shift.EndsAt);
    }
}
