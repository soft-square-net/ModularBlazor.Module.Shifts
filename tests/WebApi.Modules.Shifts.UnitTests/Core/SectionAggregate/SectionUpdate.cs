using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.SectionAggregate;

public class SectionUpdate
{
    [Fact]
    public void KeepsTrimmedDistinctCategoriesAsGrantScopes()
    {
        var section = Section.Create(" Politics desk ", ["politics", " economy ", "politics", ""], null, null);

        section.Name.ShouldBe("Politics desk");
        section.Scopes.ShouldBe(["category:politics", "category:economy"]);
    }

    [Fact]
    public void ReplacesTheCategories()
    {
        var section = Section.Create("Desk", ["politics", "economy"], null, null);

        section.Update("Desk", ["economy", "sport"], null, null);

        section.Categories.Select(c => c.CategoryId).ShouldBe(["economy", "sport"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NeedsAName(string name) => Should.Throw<DomainException>(() => Section.Create(name, ["politics"], null, null));

    [Fact]
    public void CoversAtLeastOneCategory() => Should.Throw<DomainException>(() => Section.Create("Desk", [" "], null, null));

    [Fact]
    public void RejectsNegativeDelays() => Should.Throw<DomainException>(() => Section.Create("Desk", ["politics"], -1, null));

    [Fact]
    public void OverridesFallBackToTheModuleSettings()
    {
        var plain = Section.Create("Desk", ["politics"], null, null);
        var custom = Section.Create("Desk", ["politics"], 2, 30);

        plain.AutoAssignDelayOr(TimeSpan.FromMinutes(5)).ShouldBe(TimeSpan.FromMinutes(5));
        plain.CheckoutGraceOr(TimeSpan.FromMinutes(15)).ShouldBe(TimeSpan.FromMinutes(15));
        custom.AutoAssignDelayOr(TimeSpan.FromMinutes(5)).ShouldBe(TimeSpan.FromMinutes(2));
        custom.CheckoutGraceOr(TimeSpan.FromMinutes(15)).ShouldBe(TimeSpan.FromMinutes(30));
    }
}
