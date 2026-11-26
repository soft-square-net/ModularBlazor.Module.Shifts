using Ardalis.Specification;
using Microsoft.Extensions.Logging.Abstractions;
using soft.square.net.Organization.Contracts;
using soft.square.net.WebApi.Contracts.Messaging;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Sections.Move;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Units;
using static soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases.Units.OrgUnits;
using OrgApi = soft.square.net.WebApi.Modules.Organization.Client;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases.Sections;

/// <summary>"Move sections to Organization" against a fake Organization client.</summary>
public class MoveSectionsHandlerHandle
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly ShiftsFixture _fixture = new("admin-1");
    private readonly OrgApi.IOrganizationClient _central = Substitute.For<OrgApi.IOrganizationClient>();
    private readonly IOrganizationAccess _organization = Substitute.For<IOrganizationAccess>();
    private readonly IIntegrationEvents _events = Substitute.For<IIntegrationEvents>();
    private readonly IRepository<SectionUnitMap> _maps = Substitute.For<IRepository<SectionUnitMap>>();
    private readonly List<SectionUnitMap> _savedMaps = [];
    private readonly List<OrgApi.CreateUnitRequest> _created = [];
    private readonly Section _politics = Section.Create("Politics", ["politics", "economy"], null, null).WithId(1);
    private readonly Section _seo = Section.CreateDepartment("SEO").WithId(2);
    private readonly Shift _politicsShift;
    private readonly MoveSectionsToOrganizationHandler _handler;

    public MoveSectionsHandlerHandle()
    {
        _fixture.SectionReader.ListAsync(Arg.Any<ISpecification<Section>>(), Arg.Any<CancellationToken>()).Returns([_politics, _seo]);
        _politicsShift = ShiftBuilder.Published(_fixture.Clock.Now.AddHours(1));
        _fixture.Shifts.ListAsync(Arg.Any<ISpecification<Shift>>(), Arg.Any<CancellationToken>())
            .Returns(c => c.Arg<ISpecification<Shift>>() is var spec && Matches(spec, _politics.Id) ? [_politicsShift] : new List<Shift>());

        _maps.ListAsync(Arg.Any<ISpecification<SectionUnitMap>>(), Arg.Any<CancellationToken>()).Returns(_ => _savedMaps.ToList());
        _maps.AddAsync(Arg.Any<SectionUnitMap>(), Arg.Any<CancellationToken>()).Returns(c => { _savedMaps.Add(c.Arg<SectionUnitMap>()); return c.Arg<SectionUnitMap>(); });

        _organization.Configured.Returns(true);
        _organization.CentralAsCaller().Returns(_central);
        _central.ListUnitsAsync(Org, Arg.Any<CancellationToken>()).Returns(new List<OrgApi.OrgUnitDto>());
        _central.CreateUnitAsync(Org, Arg.Any<OrgApi.CreateUnitRequest>(), Arg.Any<CancellationToken>()).Returns(c =>
        {
            var request = c.Arg<OrgApi.CreateUnitRequest>();
            _created.Add(request);
            return Unit(Guid.NewGuid(), request.NameEn, request.Tags ?? [], request.Categories ?? []);
        });

        _handler = new MoveSectionsToOrganizationHandler(_fixture.SectionReader, _maps, _fixture.Shifts, _fixture.Units, _organization, _events,
            _fixture.ShiftGrants, _fixture.Caller, NullLogger<MoveSectionsToOrganizationHandler>.Instance);
    }

    /// <summary>The shift specifications of the handler filter by section; the fake answers for the politics section only.</summary>
    private static bool Matches(ISpecification<Shift> spec, int sectionId) => spec.Evaluate([ShiftWithSection(sectionId)]).Any();

    private static Shift ShiftWithSection(int sectionId) =>
        Shift.Create(sectionId, TestClock.Start.AddDays(1), TestClock.Start.AddDays(1).AddHours(1), [("m", ShiftRole.Manager)]);

    private static OrgApi.OrgUnitDto Unit(Guid id, string name, IReadOnlyList<string> tags, IReadOnlyList<string> categories) => new()
    {
        Id = id, OrgId = Org, ParentId = Department, TypeKey = "section", Path = PathOf(Root, Department, id), NameEn = name, NameAr = name,
        Tags = tags, Categories = categories, Version = 1
    };

    private async Task WithDepartment() => await _fixture.Units.ApplyAsync(State(DepartmentUnit()), Ct);

    private Task<Result<SectionMoveReport>> Move(Guid? parent = null, IReadOnlyList<SectionUnitLink>? links = null) =>
        _handler.Handle(new MoveSectionsToOrganizationCommand(parent, links), Ct).AsTask();

    [Fact]
    public async Task EachSectionBecomesASectionUnitUnderTheDepartment()
    {
        await WithDepartment();

        var result = await Move(Department);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Sections.Select(s => (s.SectionId, s.Status)).ShouldBe([(1, SectionMoveStatus.Created), (2, SectionMoveStatus.Created)]);
        _created.Count.ShouldBe(2);
        _created[0].ShouldSatisfyAllConditions(
            r => r.ParentId.ShouldBe(Department),
            r => r.TypeKey.ShouldBe("section"),
            r => r.NameEn.ShouldBe("Politics"),
            r => r.NameAr.ShouldBe("Politics"),
            r => r.Tags!.ShouldBeEmpty(),
            r => r.Categories!.ShouldBe(["politics", "economy"]));
        // D1: a department section serves every category.
        _created[1].Tags!.ShouldBe(["department"]);
        _created[1].Categories!.ShouldBe(["*"]);
    }

    [Fact]
    public async Task AMoveMapsTheSectionMovesItsShiftsPublishesTheEventAndRegrantsOnTheUnit()
    {
        await WithDepartment();

        var result = await Move(Department);

        var politics = result.Value.Sections[0];
        var unitId = politics.UnitId!.Value;
        politics.ShiftsMoved.ShouldBe(1);
        _politicsShift.UnitId.ShouldBe(unitId);
        _savedMaps.Select(m => (m.SectionId, m.MovedBy)).ShouldBe([(1, "admin-1"), (2, "admin-1")]);
        _savedMaps[0].UnitId.ShouldBe(unitId);
        (await _fixture.Units.FindAsync(unitId, Ct)).ShouldNotBeNull(); // cached at once

        _events.Received(1).Publish("shifts.section-moved.v1", new SectionMoved(1, unitId), "section:1",
            PathOf(Root, Department, unitId), UnitPaths.Id(Org));
        _events.Received(1).Publish("shifts.section-moved.v1", new SectionMoved(2, result.Value.Sections[1].UnitId!.Value), "section:2",
            Arg.Any<string?>(), Arg.Any<string?>());

        _fixture.WrittenGrants(_politicsShift).Select(g => g.Scope).Distinct()
            .ShouldBe([$"unit:{UnitPaths.Id(unitId)}", "category:politics", "category:economy"]);
    }

    [Fact]
    public async Task ASecondRunCreatesNothingAndReportsEverySectionAsMapped()
    {
        await WithDepartment();
        var first = await Move(Department);
        _events.ClearReceivedCalls();

        var second = await Move(Department);

        second.Value.Sections.Select(s => s.Status).ShouldAllBe(s => s == SectionMoveStatus.AlreadyMapped);
        second.Value.Sections.Select(s => s.UnitId).ShouldBe(first.Value.Sections.Select(s => s.UnitId));
        second.Value.Sections.Select(s => s.ShiftsMoved).ShouldAllBe(n => n == 0);
        _created.Count.ShouldBe(2);
        _events.DidNotReceiveWithAnyArgs().Publish(default!, default!, default!);
        _savedMaps.Count.ShouldBe(2);
    }

    [Fact]
    public async Task AFullyMappedTenantNeedsNoParent()
    {
        await WithDepartment();
        await Move(Department);

        var again = await Move(parent: null);

        again.IsSuccess.ShouldBeTrue();
        again.Value.Sections.ShouldAllBe(s => s.Status == SectionMoveStatus.AlreadyMapped);
    }

    [Fact]
    public async Task ARefusedWildcardFailsOnlyTheDepartmentSectionAndSaysWhy()
    {
        await WithDepartment();
        _central.CreateUnitAsync(Org, Arg.Is<OrgApi.CreateUnitRequest>(r => r.Categories!.Contains("*")), Arg.Any<CancellationToken>())
            .Returns<OrgApi.OrgUnitDto>(_ => throw new OrgApi.OrganizationApiException("Bad", 400, "{\"detail\":\"Unknown category '*'.\"}",
                new Dictionary<string, IEnumerable<string>>(), null));

        var result = await Move(Department);

        result.Value.Sections[0].Status.ShouldBe(SectionMoveStatus.Created);
        var seo = result.Value.Sections[1];
        seo.Status.ShouldBe(SectionMoveStatus.Failed);
        seo.UnitId.ShouldBeNull();
        var detail = seo.Detail.ShouldNotBeNull();
        detail.ShouldContain("400");
        detail.ShouldContain("Unknown category '*'");
        detail.ShouldContain("wildcard category '*' (decision D1)");
        _savedMaps.Select(m => m.SectionId).ShouldBe([1]);
    }

    [Fact]
    public async Task AUnitLeftByAnInterruptedRunIsReused()
    {
        await WithDepartment();
        var leftOver = Unit(Guid.NewGuid(), "Politics", [], ["politics", "economy"]);
        _central.ListUnitsAsync(Org, Arg.Any<CancellationToken>()).Returns([leftOver]);

        var result = await Move(Department);

        result.Value.Sections[0].ShouldSatisfyAllConditions(
            s => s.Status.ShouldBe(SectionMoveStatus.Created),
            s => s.UnitId.ShouldBe(leftOver.Id),
            s => s.Detail!.ShouldContain("Reused"));
        _created.Select(r => r.NameEn).ShouldBe(["SEO"]);
    }

    [Fact]
    public async Task LinksMoveSectionsToUnitsMadeByHandWithoutCallingOrganization()
    {
        _organization.Configured.Returns(false);
        await _fixture.Units.ApplyAsync(State(SportUnit(1, "Politics", "politics", "economy")), Ct);
        await _fixture.Units.ApplyAsync(State(SeoUnit()), Ct);

        var result = await Move(links: [new SectionUnitLink(1, Sport), new SectionUnitLink(2, Seo)]);

        result.Value.Sections.Select(s => (s.Status, s.UnitId)).ShouldBe([(SectionMoveStatus.Linked, (Guid?)Sport), (SectionMoveStatus.Linked, (Guid?)Seo)]);
        _organization.DidNotReceive().CentralAsCaller();
        _events.Received(1).Publish("shifts.section-moved.v1", new SectionMoved(2, Seo), "section:2", PathOf(Root, Department, Seo), UnitPaths.Id(Org));
    }

    [Fact]
    public async Task ALinkToAUnitThatIsNotCachedFails()
    {
        await WithDepartment();

        var result = await Move(Department, [new SectionUnitLink(2, Guid.NewGuid())]);

        result.Value.Sections[1].Status.ShouldBe(SectionMoveStatus.Failed);
        result.Value.Sections[1].Detail!.ShouldContain("resync");
    }

    [Fact]
    public async Task TheParentMustBeACachedDepartment()
    {
        await _fixture.Units.ApplyAsync(State(SportUnit()), Ct);

        (await Move(parent: null)).Status.ShouldBe(ResultStatus.Invalid);
        (await Move(Sport)).Status.ShouldBe(ResultStatus.Invalid);       // a Section, not a Department
        (await Move(Guid.NewGuid())).Status.ShouldBe(ResultStatus.Invalid); // unknown
        _created.ShouldBeEmpty();
    }

    [Fact]
    public async Task WithoutOrganizationTheMoveIsUnavailable()
    {
        await WithDepartment();
        _organization.Configured.Returns(false);

        (await Move(Department)).Status.ShouldBe(ResultStatus.Unavailable);
    }
}
