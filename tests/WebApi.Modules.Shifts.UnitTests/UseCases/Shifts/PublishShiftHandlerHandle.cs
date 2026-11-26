using soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Publish;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases.Shifts;

public class PublishShiftHandlerHandle
{
    private readonly ShiftsFixture _fixture = new("admin-1");
    private readonly PublishShiftHandler _handler;

    public PublishShiftHandlerHandle()
    {
        _fixture.ShiftAdmin();
        _handler = new PublishShiftHandler(_fixture.Changes, _fixture.Authority, _fixture.Notices);
    }

    [Fact]
    public async Task IsNotFoundWithoutTheShift()
    {
        var result = await _handler.Handle(new PublishShiftCommand(9), TestContext.Current.CancellationToken);

        result.Status.ShouldBe(ResultStatus.NotFound);
        await _fixture.Grants.DidNotReceiveWithAnyArgs().ReplaceSourceAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GrantsNobodyAnythingAndReportsTheShift()
    {
        var shift = ShiftBuilder.Planned(_fixture.Clock.Now.AddHours(1));
        _fixture.Stored(shift);

        var result = await _handler.Handle(new PublishShiftCommand(shift.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Published.ShouldBeTrue();
        result.Value.Rota.ShouldAllBe(m => m.Status == AttendanceStatuses.Expected && m.AccessFrom == null);
        await _fixture.Shifts.Received(1).UpdateAsync(shift, Arg.Any<CancellationToken>());
        _fixture.WrittenGrants(shift).ShouldBeEmpty();
        var published = _fixture.Published.Single();
        (published.Type, published.Subject).ShouldBe((ShiftsEvents.Published, $"shift:{shift.Id}"));
        ((ShiftPublished)published.Data).Shift.Members.Count.ShouldBe(3);

        // Publishing again reports nothing more.
        await _handler.Handle(new PublishShiftCommand(shift.Id), TestContext.Current.CancellationToken);
        _fixture.Published.Count.ShouldBe(1);
    }

    [Fact]
    public async Task OnceCheckedInEachRoleHasItsPermissionsOnEveryCategoryUntilTheEndPlusTheGrace()
    {
        var shift = ShiftBuilder.CheckedIn(_fixture.Clock.Now.AddHours(1));

        await _fixture.ShiftGrants.WriteAsync(shift, _fixture.Section, TestContext.Current.CancellationToken);

        var grants = _fixture.WrittenGrants(shift);
        // Manager: accept, assign, transfer, return; Desk: publish, return; Editor: review. Two categories each.
        grants.Count.ShouldBe((4 + 2 + 1) * 2);
        var end = shift.EndsAt + ShiftBuilder.Grace;
        grants.ShouldContain(new NewGrant(ShiftBuilder.Editor, "content.review", "category:politics", shift.StartsAt, end));
        grants.ShouldContain(new NewGrant(ShiftBuilder.Desk, "content.publish", "category:economy", shift.StartsAt, end));
        grants.ShouldAllBe(g => g.ValidFrom == shift.StartsAt && g.ValidTo == end);
    }
}
