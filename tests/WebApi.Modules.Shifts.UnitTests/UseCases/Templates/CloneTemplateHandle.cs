using Ardalis.Specification;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.TemplateAggregate;
using soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Templates;
using static soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases.Units.OrgUnits;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases.Templates;

/// <summary>
/// Cloning a template into a Draft shift: the window in the unit's time zone (across a DST change), the default members
/// that still hold, and warnings for what does not.
/// </summary>
public class CloneTemplateHandle
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly ShiftsFixture _fixture = new("dept-manager");
    private readonly IReadRepository<ShiftTemplate> _templates = Substitute.For<IReadRepository<ShiftTemplate>>();
    private readonly IReadRepository<SectionUnitMap> _maps = Substitute.For<IReadRepository<SectionUnitMap>>();

    public CloneTemplateHandle()
    {
        _fixture.Shifts.AddAsync(Arg.Any<Shift>(), Arg.Any<CancellationToken>()).Returns(c => c.Arg<Shift>());
        _maps.ListAsync(Arg.Any<ISpecification<SectionUnitMap>>(), Arg.Any<CancellationToken>()).Returns([]);
        _fixture.Manages(Department);
    }

    private async Task Units(string? departmentZone, string? sportZone = null)
    {
        await _fixture.Units.ApplyAsync(State(RootUnit()) with { TimeZone = null }, Ct);
        await _fixture.Units.ApplyAsync(State(DepartmentUnit()) with { TimeZone = departmentZone }, Ct);
        await _fixture.Units.ApplyAsync(State(SportUnit()) with { TimeZone = sportZone }, Ct);
    }

    private Task<Result<TemplateCloneResult>> Clone(ShiftTemplate template, DateOnly date)
    {
        template.WithId(5);
        _templates.FirstOrDefaultAsync(Arg.Any<ISpecification<ShiftTemplate>>(), Arg.Any<CancellationToken>()).Returns(template);
        return new CloneTemplateHandler(_templates, _maps, _fixture.Shifts, _fixture.Units, _fixture.Authority, _fixture.Roles, _fixture.UnitMembers,
            _fixture.Details).Handle(new CloneTemplateCommand(5, date), Ct).AsTask();
    }

    private static ShiftTemplate Morning(Guid unitId, params (string UserId, string RoleKey)[] members) =>
        ShiftTemplate.Create(unitId, "Morning", new TimeOnly(6, 0), TimeSpan.FromHours(8), [("manager", 1), ("desk", 2)], members);

    [Fact]
    public async Task TheSameMorningOnEitherSideOfTheClockChangeStartsAtSixOnTheClock()
    {
        await Units(departmentZone: null, sportZone: "Europe/Berlin");
        _fixture.Member(Sport, "m");

        var before = (await Clone(Morning(Sport, ("m", "manager")), new DateOnly(2026, 3, 28))).Value.Shift;
        var after = (await Clone(Morning(Sport, ("m", "manager")), new DateOnly(2026, 3, 29))).Value.Shift;

        before.StartsAt.ShouldBe(new DateTimeOffset(2026, 3, 28, 5, 0, 0, TimeSpan.Zero));
        after.StartsAt.ShouldBe(new DateTimeOffset(2026, 3, 29, 4, 0, 0, TimeSpan.Zero));
        (after.EndsAt - after.StartsAt).ShouldBe(TimeSpan.FromHours(8));
    }

    [Fact]
    public async Task AUnitWithoutAZoneUsesTheNearestOneAboveIt()
    {
        await Units(departmentZone: "Europe/Berlin");

        var clone = (await Clone(Morning(Sport), new DateOnly(2026, 10, 26))).Value;

        clone.Shift.StartsAt.ShouldBe(new DateTimeOffset(2026, 10, 26, 5, 0, 0, TimeSpan.Zero)); // 06:00 CET
        clone.Warnings.ShouldNotContain(w => w.Contains("UTC"));
    }

    [Fact]
    public async Task WithNoZoneAnywhereTheTimesAreUtcWithAWarning()
    {
        await Units(departmentZone: null);

        var clone = (await Clone(Morning(Sport), new DateOnly(2026, 3, 29))).Value;

        clone.Shift.StartsAt.ShouldBe(new DateTimeOffset(2026, 3, 29, 6, 0, 0, TimeSpan.Zero));
        clone.Warnings.ShouldContain(w => w.Contains("UTC"));
    }

    [Fact]
    public async Task TheCloneIsADraftAndSaysWhatItCouldNotFill()
    {
        await Units(departmentZone: "Europe/Berlin");
        _fixture.Member(Sport, "d1");
        _fixture.Member(Department, "m1");
        _fixture.Member(Department, "m2");
        var template = Morning(Sport, ("d1", "desk"), ("left", "desk"), ("x", "pilot"), ("m1", "manager"), ("m2", "manager"));

        var clone = (await Clone(template, new DateOnly(2026, 11, 30))).Value;

        clone.Shift.Published.ShouldBeFalse();
        clone.Shift.UnitId.ShouldBe(Sport);
        clone.Shift.Rota.Select(r => (r.UserId, r.RoleKey, r.IsLead)).ShouldBe([("m1", "manager", true), ("d1", "desk", false)], ignoreOrder: true);
        clone.Warnings.ShouldContain(w => w.StartsWith("left was left out: not a member"));
        clone.Warnings.ShouldContain(w => w.StartsWith("x was left out: no role 'pilot'"));
        clone.Warnings.ShouldContain(w => w.StartsWith("m2 was left out: the shift has a lead already"));
        clone.Warnings.ShouldContain("Slot 'desk': 1 of 2 filled.");
        clone.Warnings.ShouldNotContain(w => w.Contains("no lead"));
    }

    [Fact]
    public async Task ACloneWithoutALeadSaysSoAndUsesTheUnitsSection()
    {
        await Units(departmentZone: "Europe/Berlin");
        _fixture.Section.WithId(3);
        _maps.ListAsync(Arg.Any<ISpecification<SectionUnitMap>>(), Arg.Any<CancellationToken>())
            .Returns([new SectionUnitMap(3, Sport, _fixture.Clock.Now, "admin-1")]);

        var clone = (await Clone(Morning(Sport), new DateOnly(2026, 11, 30))).Value;

        clone.Shift.SectionId.ShouldBe(3);
        clone.Shift.Rota.ShouldBeEmpty();
        clone.Warnings.ShouldContain(w => w.Contains("no lead"));
        clone.Warnings.ShouldContain("Slot 'manager': 0 of 1 filled.");
    }

    [Fact]
    public async Task OnlyAManagerOfTheUnitClones()
    {
        await Units(departmentZone: null);
        await _fixture.Units.ApplyAsync(State(Snapshot(OtherDepartment, PathOf(Root, OtherDepartment), Root, "department", 1)), Ct);

        (await Clone(Morning(OtherDepartment), new DateOnly(2026, 11, 30))).Status.ShouldBe(ResultStatus.Forbidden);
        (await Clone(Morning(Guid.NewGuid()), new DateOnly(2026, 11, 30))).Status.ShouldBe(ResultStatus.Conflict); // unit not cached
    }
}
