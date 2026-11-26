using soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Update;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases.Shifts;

public class UpdateShiftHandlerHandle
{
    private readonly ShiftsFixture _fixture = new("admin-1");

    public UpdateShiftHandlerHandle() => _fixture.ShiftAdmin();

    private UpdateShiftHandler Handler => new(_fixture.Changes, _fixture.ShiftUnits, _fixture.Rota, _fixture.Authority);

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
    public async Task ANewRotaOfAPublishedShiftRewritesItsGrants()
    {
        var shift = ShiftBuilder.Published(_fixture.Clock.Now.AddHours(1));
        _fixture.Stored(shift);

        var result = await Handler.Handle(
            new UpdateShiftCommand(shift.Id, shift.SectionId!.Value, shift.StartsAt, shift.EndsAt, [new RotaEntry("new-manager", "manager")]),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        _fixture.WrittenGrants(shift).Select(g => g.UserId).Distinct().ShouldBe(["new-manager"]);
    }
}
