using soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.CheckOutMember;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases.Shifts;

public class CheckOutMemberHandlerHandle
{
    [Fact]
    public async Task AMemberChecksThemselvesOutAndOnlyTheirGrantsEnd()
    {
        var fixture = new ShiftsFixture(ShiftBuilder.Editor);
        var shift = ShiftBuilder.Published(fixture.Clock.Now.AddHours(-1));
        fixture.Stored(shift);

        var result = await new CheckOutMemberHandler(fixture.Changes, fixture.Caller, fixture.Authority)
            .Handle(new CheckOutMemberCommand(shift.Id, ShiftBuilder.Editor), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var grants = fixture.WrittenGrants(shift);
        grants.Where(g => g.UserId == ShiftBuilder.Editor).ShouldAllBe(g => g.ValidTo == fixture.Clock.Now);
        grants.Where(g => g.UserId != ShiftBuilder.Editor).ShouldAllBe(g => g.ValidTo == shift.EndsAt);
    }

    [Fact]
    public async Task AMemberCannotCheckOutSomeoneElse()
    {
        var fixture = new ShiftsFixture(ShiftBuilder.Editor);
        var shift = ShiftBuilder.Published(fixture.Clock.Now.AddHours(-1));
        fixture.Stored(shift);

        var result = await new CheckOutMemberHandler(fixture.Changes, fixture.Caller, fixture.Authority)
            .Handle(new CheckOutMemberCommand(shift.Id, ShiftBuilder.Desk), TestContext.Current.CancellationToken);

        result.Status.ShouldBe(ResultStatus.Forbidden);
    }
}
