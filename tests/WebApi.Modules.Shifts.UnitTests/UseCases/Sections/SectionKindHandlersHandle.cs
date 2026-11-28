using Ardalis.Specification;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Sections.Serving;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Publish;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases.Sections;

/// <summary>The sections that serve a category, and the rota of a department's shift.</summary>
public class SectionKindHandlersHandle
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>A tenant-wide shift admin, who may publish.</summary>
    private static ShiftsFixture Admin()
    {
        var fixture = new ShiftsFixture("admin-1");
        fixture.ShiftAdmin();
        return fixture;
    }

    [Fact]
    public async Task ADepartmentShiftGrantsTheCheckedInRotaOnTheDepartment()
    {
        var fixture = Admin();
        var department = Section.CreateDepartment("Multimedia").WithId(1);
        fixture.SectionReader.FirstOrDefaultAsync(Arg.Any<ISpecification<Section>>(), Arg.Any<CancellationToken>()).Returns(department);
        fixture.SectionReader.ListAsync(Arg.Any<ISpecification<Section>>(), Arg.Any<CancellationToken>()).Returns([department]);
        var shift = ShiftBuilder.Planned(fixture.Clock.Now.AddHours(1));
        fixture.Stored(shift);

        var result = await new PublishShiftHandler(fixture.Changes, fixture.Authority, fixture.Notices).Handle(new PublishShiftCommand(shift.Id), Ct);
        result.IsSuccess.ShouldBeTrue();
        fixture.WrittenGrants(shift).ShouldBeEmpty(); // publishing grants nobody anything
        ShiftBuilder.CheckEveryoneIn(shift, shift.StartsAt);
        await fixture.ShiftGrants.WriteAsync(shift, department, Ct);

        var grants = fixture.WrittenGrants(shift);
        grants.Count.ShouldBe(4 + 2 + 1); // the Manager, Desk and Editor permissions once each: a single scope
        grants.ShouldAllBe(g => g.Scope == "section:1");
        grants.ShouldContain(new NewGrant(ShiftBuilder.Desk, "content.publish", "section:1", shift.StartsAt, shift.EndsAt + ShiftBuilder.Grace));
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
