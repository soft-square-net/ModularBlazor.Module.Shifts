using soft.square.net.Organization.Contracts;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.UnitAggregate;
using soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;
using static soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases.Units.OrgUnits;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases.Shifts;

/// <summary>The grant scopes of a shift of an Organization unit.</summary>
public class ShiftScopesBuild
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public void ASectionUnitGrantsOnItselfAndOnEachOfItsCategories()
    {
        var unit = CachedUnit.From(State(SportUnit(1, "Sport", "football", "tennis")));

        ShiftScopes.For(unit, unit.Categories).ShouldBe([$"unit:{UnitPaths.Id(Sport)}", "category:football", "category:tennis"]);
    }

    [Fact]
    public void ADepartmentSectionSkipsTheWildcardAndGrantsOnTheSectionUnit()
    {
        var unit = CachedUnit.From(State(SeoUnit()));

        ShiftScopes.For(unit, unit.Categories).ShouldBe([$"unit:{UnitPaths.Id(Seo)}", $"section:{UnitPaths.Id(Seo)}"]);
    }

    [Fact]
    public void TheSectionsOwnScopesStayWhileTheShiftHasItsSection()
    {
        var unit = CachedUnit.From(State(SeoUnit()));
        var section = Section.CreateDepartment("SEO").WithId(7);

        ShiftScopes.For(unit, unit.Categories, section).ShouldBe([$"unit:{UnitPaths.Id(Seo)}", $"section:{UnitPaths.Id(Seo)}", "section:7"]);
    }

    [Fact]
    public void AScopeIsListedOnce()
    {
        var unit = CachedUnit.From(State(SportUnit(1, "Sport", "football")));
        var section = Section.Create("Sport", ["football"], null, null).WithId(3);

        ShiftScopes.For(unit, unit.Categories, section).ShouldBe([$"unit:{UnitPaths.Id(Sport)}", "category:football"]);
    }

    [Fact]
    public async Task APublishedShiftOfAUnitIsGrantedOnTheUnitScopesAndTheCategoriesBelowIt()
    {
        var fixture = new ShiftsFixture("admin-1");
        await fixture.Units.ApplyAsync(State(DepartmentUnit()), Ct);
        await fixture.Units.ApplyAsync(State(SportUnit(1, "Sport", "football")), Ct);
        await fixture.Units.ApplyAsync(State(SeoUnit()), Ct);
        var shift = Shift.Create(1, fixture.Clock.Now.AddHours(1), fixture.Clock.Now.AddHours(9), [ShiftBuilder.Lead(ShiftBuilder.Manager)], Department);
        shift.Publish();
        ShiftBuilder.CheckEveryoneIn(shift, shift.StartsAt);

        await fixture.ShiftGrants.WriteAsync(shift, fixture.Section, Ct);

        // The department's categories are its sections' ('*' skipped); then the section's own (politics, economy).
        fixture.WrittenGrants(shift).Select(g => g.Scope).Distinct().ShouldBe(
            [$"unit:{UnitPaths.Id(Department)}", "category:football", "category:politics", "category:economy"]);
    }

    [Fact]
    public async Task AShiftWhoseUnitIsNotCachedKeepsItsSectionsScopes()
    {
        var fixture = new ShiftsFixture("admin-1");
        var shift = Shift.Create(1, fixture.Clock.Now.AddHours(1), fixture.Clock.Now.AddHours(9), [ShiftBuilder.Lead(ShiftBuilder.Manager)], Sport);
        shift.Publish();
        ShiftBuilder.CheckEveryoneIn(shift, shift.StartsAt);

        await fixture.ShiftGrants.WriteAsync(shift, fixture.Section, Ct);

        fixture.WrittenGrants(shift).Select(g => g.Scope).Distinct().ShouldBe(["category:politics", "category:economy"]);
    }
}
