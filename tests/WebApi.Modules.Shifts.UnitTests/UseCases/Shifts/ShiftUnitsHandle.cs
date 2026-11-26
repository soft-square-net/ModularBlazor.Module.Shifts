using Ardalis.Specification;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Sections;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Sections.Add;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Sections.Update;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Add;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Update;
using static soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases.Units.OrgUnits;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases.Shifts;

/// <summary>Every new shift names a cached unit; the sections are read-only once moved.</summary>
public class ShiftUnitsHandle
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly ShiftsFixture _fixture = new("admin-1");
    private readonly AddShiftHandler _add;

    public ShiftUnitsHandle()
    {
        _fixture.SectionReader.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(_fixture.Section);
        _fixture.Shifts.AddAsync(Arg.Any<Shift>(), Arg.Any<CancellationToken>()).Returns(c => c.Arg<Shift>());
        _add = new AddShiftHandler(_fixture.Shifts, _fixture.SectionReader, _fixture.ShiftUnits, _fixture.Details);
    }

    private Task<Result<ShiftDto>> Add(Guid? unitId) => _add.Handle(new AddShiftCommand(1, _fixture.Clock.Now.AddHours(1), _fixture.Clock.Now.AddHours(9),
        [new RotaEntry("m", ShiftRole.Manager)], unitId), Ct).AsTask();

    private void SectionMovedTo(Guid unitId) =>
        _fixture.Moved.FirstOrDefaultAsync(Arg.Any<ISpecification<SectionUnitMap>>(), Arg.Any<CancellationToken>())
            .Returns(new SectionUnitMap(1, unitId, _fixture.Clock.Now, "admin-1"));

    [Fact]
    public async Task ANewShiftInACachedUnitIsPlanned()
    {
        await _fixture.Units.ApplyAsync(State(SportUnit()), Ct);

        var result = await Add(Sport);

        result.IsSuccess.ShouldBeTrue();
        result.Value.UnitId.ShouldBe(Sport);
    }

    [Fact]
    public async Task ANewShiftNeedsAUnit()
    {
        var result = await Add(null);

        result.Status.ShouldBe(ResultStatus.Invalid);
        await _fixture.Shifts.DidNotReceiveWithAnyArgs().AddAsync(default!, Ct);
    }

    [Fact]
    public async Task AUnitTheModuleHasNotCachedIsRefused()
    {
        (await Add(Guid.NewGuid())).Status.ShouldBe(ResultStatus.Invalid);
    }

    [Fact]
    public async Task ARemovedUnitIsRefused()
    {
        await _fixture.Units.ApplyAsync(State(SportUnit()), Ct);
        await _fixture.Units.RemoveAsync(new(Sport, Org, PathOf(Root, Department, Sport)), Ct);

        (await Add(Sport)).Status.ShouldBe(ResultStatus.Invalid);
    }

    [Fact]
    public async Task AMovedSectionsShiftGoesToItsUnitByDefault()
    {
        await _fixture.Units.ApplyAsync(State(SportUnit()), Ct);
        SectionMovedTo(Sport);

        (await Add(null)).Value.UnitId.ShouldBe(Sport);
    }

    [Fact]
    public async Task AMovedSectionsShiftCannotNameAnotherUnit()
    {
        await _fixture.Units.ApplyAsync(State(SportUnit()), Ct);
        await _fixture.Units.ApplyAsync(State(SeoUnit()), Ct);
        SectionMovedTo(Sport);

        (await Add(Seo)).Status.ShouldBe(ResultStatus.Invalid);
    }

    [Fact]
    public async Task AnUpdateGivesAShiftWithoutAUnitItsUnitButNeverMovesOne()
    {
        await _fixture.Units.ApplyAsync(State(SportUnit()), Ct);
        await _fixture.Units.ApplyAsync(State(SeoUnit()), Ct);
        var shift = ShiftBuilder.Planned(_fixture.Clock.Now.AddHours(1));
        _fixture.Stored(shift);
        var update = new UpdateShiftHandler(_fixture.Changes, _fixture.ShiftUnits);
        UpdateShiftCommand Command(Guid unitId) =>
            new(shift.Id, shift.SectionId, shift.StartsAt, shift.EndsAt, [new RotaEntry("m", ShiftRole.Manager)], unitId);

        (await update.Handle(Command(Sport), Ct)).Value.UnitId.ShouldBe(Sport);
        (await update.Handle(Command(Seo), Ct)).Status.ShouldBe(ResultStatus.Conflict);
        shift.UnitId.ShouldBe(Sport);
    }

    [Fact]
    public async Task SectionsAreReadOnlyOnceMoved()
    {
        _fixture.Moved.AnyAsync(Arg.Any<CancellationToken>()).Returns(true);
        var sections = Substitute.For<IRepository<Section>>();

        var added = await new AddSectionHandler(sections, _fixture.Moved).Handle(new AddSectionCommand("Sport", ["sport"], null, null), Ct);
        var updated = await new UpdateSectionHandler(sections, _fixture.ShiftReader, _fixture.Moved, _fixture.ShiftGrants, _fixture.Caller)
            .Handle(new UpdateSectionCommand(1, "Sport", ["sport"], null, null), Ct);

        added.Status.ShouldBe(ResultStatus.Conflict);
        updated.Status.ShouldBe(ResultStatus.Conflict);
        added.Errors.ShouldBe([SectionDto.MovedToOrganization]);
        await sections.DidNotReceiveWithAnyArgs().AddAsync(default!, Ct);
        await sections.DidNotReceiveWithAnyArgs().UpdateAsync(default!, Ct);
    }
}
