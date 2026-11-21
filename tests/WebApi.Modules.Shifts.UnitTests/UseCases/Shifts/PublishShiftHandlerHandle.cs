using soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Publish;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases.Shifts;

public class PublishShiftHandlerHandle
{
    private readonly ShiftsFixture _fixture = new("admin-1");
    private readonly PublishShiftHandler _handler;

    public PublishShiftHandlerHandle() => _handler = new PublishShiftHandler(_fixture.Changes);

    [Fact]
    public async Task IsNotFoundWithoutTheShift()
    {
        var result = await _handler.Handle(new PublishShiftCommand(9), TestContext.Current.CancellationToken);

        result.Status.ShouldBe(ResultStatus.NotFound);
        await _fixture.Grants.DidNotReceiveWithAnyArgs().ReplaceSourceAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task WritesEachRolesPermissionsOnEveryCategoryForTheShiftOnly()
    {
        var shift = ShiftBuilder.Planned(_fixture.Clock.Now.AddHours(1));
        _fixture.Stored(shift);

        var result = await _handler.Handle(new PublishShiftCommand(shift.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Published.ShouldBeTrue();
        await _fixture.Shifts.Received(1).UpdateAsync(shift, Arg.Any<CancellationToken>());

        var grants = _fixture.WrittenGrants(shift);
        // Manager: accept, assign, transfer, return; Desk: publish, return; Editor: review. Two categories each.
        grants.Count.ShouldBe((4 + 2 + 1) * 2);
        grants.ShouldContain(new NewGrant(ShiftBuilder.Editor, "content.review", "category:politics", shift.StartsAt, shift.EndsAt));
        grants.ShouldContain(new NewGrant(ShiftBuilder.Desk, "content.publish", "category:economy", shift.StartsAt, shift.EndsAt));
        grants.ShouldAllBe(g => g.ValidFrom == shift.StartsAt && g.ValidTo == shift.EndsAt);
    }
}
