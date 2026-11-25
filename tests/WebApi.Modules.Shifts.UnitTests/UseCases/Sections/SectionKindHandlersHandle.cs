using Ardalis.Specification;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Sections.Add;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Sections.Serving;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Sections.Update;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Publish;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases.Sections;

/// <summary>Department sections through the section use cases, and the rota of a department's shift.</summary>
public class SectionKindHandlersHandle
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static IRepository<Section> AddingRepository()
    {
        var sections = Substitute.For<IRepository<Section>>();
        sections.AddAsync(Arg.Any<Section>(), Arg.Any<CancellationToken>()).Returns(c => c.Arg<Section>());
        return sections;
    }

    private static IRepository<Section> Holding(Section section)
    {
        var sections = Substitute.For<IRepository<Section>>();
        sections.FirstOrDefaultAsync(Arg.Any<ISpecification<Section>>(), Arg.Any<CancellationToken>()).Returns(section);
        return sections;
    }

    [Fact]
    public async Task AddsADepartment()
    {
        var result = await new AddSectionHandler(AddingRepository())
            .Handle(new AddSectionCommand("SEO", [], null, null, SectionKind.Department), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Kind.ShouldBe(SectionKind.Department);
        result.Value.Categories.ShouldBeEmpty();
    }

    [Fact]
    public async Task AddsACategorySectionWhenNoKindIsGiven()
    {
        var result = await new AddSectionHandler(AddingRepository()).Handle(new AddSectionCommand("Sport", ["sport"], null, null), Ct);

        result.Value.Kind.ShouldBe(SectionKind.Category);
    }

    [Fact]
    public async Task AnUpdateWithoutAKindKeepsTheDepartment()
    {
        var fixture = new ShiftsFixture("admin-1");
        fixture.ShiftReader.ListAsync(Arg.Any<ISpecification<Shift>>(), Arg.Any<CancellationToken>()).Returns([]);
        var sections = Holding(Section.CreateDepartment("Multimedia").WithId(4));

        var result = await new UpdateSectionHandler(sections, fixture.ShiftReader, fixture.ShiftGrants, fixture.Caller)
            .Handle(new UpdateSectionCommand(4, "Photo and video", [], 1, null), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Kind.ShouldBe(SectionKind.Department);
        result.Value.Name.ShouldBe("Photo and video");
    }

    [Fact]
    public async Task TurningASectionIntoADepartmentRegrantsItsShiftsOnTheSection()
    {
        var fixture = new ShiftsFixture("admin-1");
        var shift = ShiftBuilder.Published(fixture.Clock.Now.AddHours(1));
        fixture.ShiftReader.ListAsync(Arg.Any<ISpecification<Shift>>(), Arg.Any<CancellationToken>()).Returns([shift]);
        var sections = Holding(Section.Create("Desk", ["politics"], null, null).WithId(1));

        var result = await new UpdateSectionHandler(sections, fixture.ShiftReader, fixture.ShiftGrants, fixture.Caller)
            .Handle(new UpdateSectionCommand(1, "Multimedia", [], null, null, SectionKind.Department), Ct);

        result.IsSuccess.ShouldBeTrue();
        fixture.WrittenGrants(shift).Select(g => g.Scope).Distinct().ShouldBe(["section:1"]);
    }

    [Fact]
    public async Task PublishingADepartmentShiftGrantsTheRotaOnTheDepartment()
    {
        var fixture = new ShiftsFixture("admin-1");
        var department = Section.CreateDepartment("Multimedia").WithId(1);
        fixture.SectionReader.FirstOrDefaultAsync(Arg.Any<ISpecification<Section>>(), Arg.Any<CancellationToken>()).Returns(department);
        fixture.SectionReader.ListAsync(Arg.Any<ISpecification<Section>>(), Arg.Any<CancellationToken>()).Returns([department]);
        var shift = ShiftBuilder.Planned(fixture.Clock.Now.AddHours(1));
        fixture.Stored(shift);

        var result = await new PublishShiftHandler(fixture.Changes).Handle(new PublishShiftCommand(shift.Id), Ct);

        result.IsSuccess.ShouldBeTrue();
        var grants = fixture.WrittenGrants(shift);
        grants.Count.ShouldBe(4 + 2 + 1); // the Manager, Desk and Editor permissions once each: a single scope
        grants.ShouldAllBe(g => g.Scope == "section:1");
        grants.ShouldContain(new NewGrant(ShiftBuilder.Desk, "content.publish", "section:1", shift.StartsAt, shift.EndsAt));
    }

    [Fact]
    public async Task ServingListsTheCategorySectionsBeforeTheDepartments()
    {
        var sections = Substitute.For<IReadRepository<Section>>();
        sections.ListAsync(Arg.Any<ISpecification<Section>>(), Arg.Any<CancellationToken>()).Returns(
        [
            Section.CreateDepartment("SEO").WithId(3),
            Section.Create("Sport", ["sport"], null, null).WithId(1),
            Section.CreateDepartment("Multimedia").WithId(2),
        ]);

        var result = await new ListServingSectionsHandler(sections).Handle(new ListServingSectionsQuery(" sport "), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Select(s => s.Name).ShouldBe(["Sport", "Multimedia", "SEO"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task ServingNeedsACategory(string category)
    {
        var result = await new ListServingSectionsHandler(Substitute.For<IReadRepository<Section>>())
            .Handle(new ListServingSectionsQuery(category), Ct);

        result.Status.ShouldBe(ResultStatus.Invalid);
    }
}
