using soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Update;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases.Shifts;

public class UpdateShiftHandlerHandle
{
    private readonly ShiftsFixture _fixture = new("admin-1");

    public UpdateShiftHandlerHandle() => _fixture.ShiftAdmin();

    private UpdateShiftHandler Handler => new(_fixture.Changes, _fixture.ShiftUnits, _fixture.Rota, _fixture.Authority, _fixture.Notices);

    [Fact]
    public async Task AShiftCannotMoveToAnotherSection()
    {
        var shift = ShiftBuilder.Planned(_fixture.Clock.Now.AddHours(1));
        _fixture.Stored(shift);

        var result = await Handler.Handle(
            new UpdateShiftCommand(shift.Id, 2, shift.StartsAt, shift.EndsAt, [new RotaEntry("m", "manager")]),
            TestContext.Current.CancellationToken);

        result.Status.ShouldBe(ResultStatus.Conflict);
        await _fixture.Shifts.DidNotReceiveWithAnyArgs().UpdateAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ANewRotaOfAPublishedShiftRewritesItsGrantsAndReportsTheChanges()
    {
        var shift = ShiftBuilder.CheckedIn(_fixture.Clock.Now.AddMinutes(-30));
        _fixture.Stored(shift);

        var result = await Handler.Handle(
            new UpdateShiftCommand(shift.Id, shift.SectionId!.Value, shift.StartsAt, shift.EndsAt,
                [new RotaEntry("new-manager", "manager"), new RotaEntry(ShiftBuilder.Editor, "editor")]),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        // The editor stays checked in; the new manager has not checked in yet; the others left the rota.
        _fixture.WrittenGrants(shift).Select(g => g.UserId).Distinct().ShouldBe([ShiftBuilder.Editor]);
        _fixture.PublishedOf<ShiftMemberAdded>(ShiftsEvents.MemberAdded).Select(e => e.UserId).ShouldBe(["new-manager"]);
        _fixture.PublishedOf<ShiftMemberRemoved>(ShiftsEvents.MemberRemoved).Select(e => e.UserId)
            .ShouldBe([ShiftBuilder.Manager, ShiftBuilder.Desk], ignoreOrder: true);
        _fixture.PublishedOf<ShiftUpdated>(ShiftsEvents.Updated).Single().Shift.Members.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ADraftReportsNothing()
    {
        var shift = ShiftBuilder.Planned(_fixture.Clock.Now.AddHours(1));
        _fixture.Stored(shift);

        (await Handler.Handle(new UpdateShiftCommand(shift.Id, shift.SectionId!.Value, shift.StartsAt, shift.EndsAt, [new RotaEntry("m", "manager")]),
            TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        _fixture.Published.ShouldBeEmpty();
    }
}
