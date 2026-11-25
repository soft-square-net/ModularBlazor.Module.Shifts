using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.SectionAggregate;

/// <summary>A department: a section that serves every category, with no category rows of its own.</summary>
public class SectionDepartment
{
    [Fact]
    public void ADepartmentHasNoCategoriesAndServesEveryOne()
    {
        var multimedia = Section.CreateDepartment(" Multimedia ");

        multimedia.Name.ShouldBe("Multimedia");
        multimedia.Kind.ShouldBe(SectionKind.Department);
        multimedia.IsDepartment.ShouldBeTrue();
        multimedia.Categories.ShouldBeEmpty();
        multimedia.Serves("sport").ShouldBeTrue();
        multimedia.Serves("politics").ShouldBeTrue();
    }

    [Fact]
    public void ACategorySectionServesOnlyItsCategories()
    {
        var sport = Section.Create("Sport", ["sport", "football"], null, null);

        sport.Kind.ShouldBe(SectionKind.Category);
        sport.Serves("football").ShouldBeTrue();
        sport.Serves("politics").ShouldBeFalse();
    }

    [Fact]
    public void ADepartmentTakesNoCategories() =>
        Should.Throw<DomainException>(() => Section.Create("SEO", ["politics"], null, null, SectionKind.Department));

    [Fact]
    public void BlankCategoryIdsDoNotCountAsCategories() =>
        Section.Create("SEO", [" ", ""], null, null, SectionKind.Department).Categories.ShouldBeEmpty();

    [Fact]
    public void ADepartmentIsGrantedOnItself() =>
        Section.CreateDepartment("Multimedia").WithId(7).Scopes.ShouldBe(["section:7"]);

    [Fact]
    public void ACategorySectionBecomesADepartmentWhenItsCategoriesGo()
    {
        var section = Section.Create("Desk", ["politics", "economy"], 2, null).WithId(3);

        Should.Throw<DomainException>(() => section.Update("Desk", ["politics"], 2, null, SectionKind.Department));
        section.Update("Multimedia", [], 2, null, SectionKind.Department);

        section.IsDepartment.ShouldBeTrue();
        section.Categories.ShouldBeEmpty();
        section.AutoAssignDelayMinutes.ShouldBe(2);
        section.Scopes.ShouldBe(["section:3"]);
    }

    [Fact]
    public void ADepartmentBecomesACategorySectionWithCategories()
    {
        var section = Section.CreateDepartment("Multimedia");

        Should.Throw<DomainException>(() => section.Update("Multimedia", [], null, null, SectionKind.Category));
        section.Update("Sport", ["sport"], null, null, SectionKind.Category);

        section.Kind.ShouldBe(SectionKind.Category);
        section.Scopes.ShouldBe(["category:sport"]);
    }

    [Fact]
    public void RejectsAnUnknownKind() =>
        Should.Throw<DomainException>(() => Section.Create("Desk", ["politics"], null, null, (SectionKind)9));
}
