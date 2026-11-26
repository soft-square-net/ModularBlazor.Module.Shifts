using Microsoft.Extensions.Logging.Abstractions;
using soft.square.net.Organization.Contracts;
using soft.square.net.WebApi.Contracts.Messaging;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Units;
using static soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases.Units.OrgUnits;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases.Units;

/// <summary>The unit cache fed by org.* events: order, duplicates, stale versions, moves and removals.</summary>
public class UnitCacheEvents
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly FakeUnitCacheStore _store = new();
    private readonly IOrganizationAccess _organization = Substitute.For<IOrganizationAccess>();
    private readonly UnitCache _cache;
    private readonly OrgEventsHandler _handler;

    public UnitCacheEvents()
    {
        _cache = new UnitCache(_store);
        _handler = new OrgEventsHandler(_cache, _organization, NullLogger<OrgEventsHandler>.Instance);
    }

    private Task Handle(IntegrationEnvelope envelope) => _handler.HandleAsync(envelope, Ct);

    [Fact]
    public async Task ACreatedUnitIsCachedWithEverythingItsSnapshotCarries()
    {
        await Handle(Created(SeoUnit()));

        var unit = _store.Rows[Seo];
        unit.Path.ShouldBe(PathOf(Root, Department, Seo));
        unit.ParentId.ShouldBe(Department);
        unit.OrgId.ShouldBe(Org);
        unit.TypeKey.ShouldBe("section");
        unit.NameEn.ShouldBe("SEO");
        unit.NameAr.ShouldBe("SEO (ar)");
        unit.TimeZone.ShouldBe("Africa/Cairo");
        unit.Tags.ShouldBe(["department"]);
        unit.Categories.ShouldBe(["*"]);
        unit.IsDepartmentSection.ShouldBeTrue();
        unit.Version.ShouldBe(1);
        _store.Saves.ShouldBe(0); // the platform saves a handler's changes with the inbox row
    }

    [Fact]
    public async Task EventsApplyInVersionOrder()
    {
        await Handle(Created(SportUnit(1, "Sport")));
        await Handle(Updated(SportUnit(2, "Sports")));
        await Handle(Updated(SportUnit(3, "Sports desk", "football")));

        _store.Rows[Sport].NameEn.ShouldBe("Sports desk");
        _store.Rows[Sport].Categories.ShouldBe(["football"]);
        _store.Rows[Sport].Version.ShouldBe(3);
    }

    [Fact]
    public async Task ASecondDeliveryChangesNothing()
    {
        var update = Updated(SportUnit(2, "Sports"));
        await Handle(Created(SportUnit(1, "Sport")));
        await Handle(update);
        await Handle(update);

        _store.Rows[Sport].NameEn.ShouldBe("Sports");
        _store.Rows[Sport].Version.ShouldBe(2);
        _store.Rows.Count.ShouldBe(1);
    }

    [Fact]
    public async Task AStaleVersionIsIgnored()
    {
        await Handle(Updated(SportUnit(3, "Newest")));
        await Handle(Updated(SportUnit(2, "Older")));
        await Handle(Created(SportUnit(1, "Oldest"))); // the created event arrives last

        _store.Rows[Sport].NameEn.ShouldBe("Newest");
        _store.Rows[Sport].Version.ShouldBe(3);
    }

    [Fact]
    public async Task AMoveRebasesTheWholeSubtree()
    {
        await Handle(Created(DepartmentUnit()));
        await Handle(Created(SportUnit()));
        var oldPath = PathOf(Root, Department);
        var newPath = PathOf(Root, OtherDepartment, Department);

        await Handle(Moved(Department, Root, OtherDepartment, oldPath, newPath, 2));

        _store.Rows[Department].Path.ShouldBe(newPath);
        _store.Rows[Department].ParentId.ShouldBe(OtherDepartment);
        _store.Rows[Sport].Path.ShouldBe(newPath + "/" + UnitPaths.Id(Sport));
        _store.Rows[Sport].Version.ShouldBe(1); // a descendant's own version does not change on an ancestor's move
    }

    [Fact]
    public async Task ALateMoveDoesNotDragTheSubtreeBack()
    {
        var first = PathOf(Root, Department);
        var second = PathOf(Root, OtherDepartment, Department);
        await Handle(Created(DepartmentUnit()));
        await Handle(Created(SportUnit()));

        await Handle(Moved(Department, OtherDepartment, Root, second, first, 3));  // back home (v3) arrives first...
        await Handle(Moved(Department, Root, OtherDepartment, first, second, 2));  // ...then the older move (v2)

        _store.Rows[Department].Path.ShouldBe(first);
        _store.Rows[Sport].Path.ShouldBe(first + "/" + UnitPaths.Id(Sport));
    }

    [Fact]
    public async Task ANewerSnapshotWithAnotherPathMovesTheSubtreeToo()
    {
        await Handle(Created(DepartmentUnit()));
        await Handle(Created(SportUnit()));
        var newPath = PathOf(Root, OtherDepartment, Department);

        await Handle(Updated(Snapshot(Department, newPath, OtherDepartment, "department", 3, "News")));

        _store.Rows[Sport].Path.ShouldBe(newPath + "/" + UnitPaths.Id(Sport));
    }

    [Fact]
    public async Task ARemovedUnitStaysRemovedWhateverArrivesLater()
    {
        await Handle(Created(SportUnit(1)));
        await Handle(Removed(Sport, PathOf(Root, Department, Sport)));
        await Handle(Updated(SportUnit(5, "Resurrected")));

        _store.Rows[Sport].Removed.ShouldBeTrue();
        _store.Rows[Sport].NameEn.ShouldBe("Sport");
        (await _cache.FindAsync(Sport, Ct)).ShouldBeNull();
        (await _cache.ListAsync(Ct)).ShouldBeEmpty();
    }

    [Fact]
    public async Task ARemovalBeforeTheCreationLeavesATombstone()
    {
        await Handle(Removed(Sport, PathOf(Root, Department, Sport)));
        await Handle(Created(SportUnit(1)));

        (await _cache.FindAsync(Sport, Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task AMembershipOnAnUnknownUnitReadsTheUnitAsTheServiceClient()
    {
        var directory = Substitute.For<IUnitDirectory>();
        directory.GetAsync(Sport, Arg.Any<CancellationToken>()).Returns(Directory(SportUnit(4)));
        _organization.CanActAsService.Returns(true);
        _organization.DirectoryAsService().Returns(directory);

        await Handle(MembershipOn(Sport, PathOf(Root, Department, Sport)));
        await Handle(MembershipOn(Sport, PathOf(Root, Department, Sport))); // known now: not read again

        _store.Rows[Sport].Version.ShouldBe(4);
        await directory.Received(1).GetAsync(Sport, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AMembershipChangesNothingWithoutAServiceClient()
    {
        _organization.CanActAsService.Returns(false);

        await Handle(MembershipOn(Sport, PathOf(Root, Department, Sport)));

        _store.Rows.ShouldBeEmpty();
        _organization.DidNotReceive().DirectoryAsService();
    }

    [Fact]
    public async Task AResyncAppliesTheDirectoryAndDropsWhatItNoLongerLists()
    {
        await Handle(Created(SportUnit(3, "Sport v3")));
        await Handle(Created(DepartmentUnit()));
        await Handle(Removed(Seo, PathOf(Root, Department, Seo))); // a tombstone stays

        var result = await _cache.ResyncAsync([Directory(RootUnit()), Directory(SportUnit(2, "Sport v2"))], Ct);

        result.ShouldBe(new UnitResyncResult(Listed: 2, Changed: 1, Deleted: 1));
        _store.Rows.Keys.ShouldBe([Sport, Seo, Root], ignoreOrder: true);
        _store.Rows[Sport].NameEn.ShouldBe("Sport v3"); // the directory's older version does not win either
    }

    [Fact]
    public async Task CategoriesOfAUnitWithoutItsOwnAreThoseOfItsUnitsBelow()
    {
        await Handle(Created(DepartmentUnit()));
        await Handle(Created(SportUnit(1, "Sport", "football", "tennis")));
        await Handle(Created(Snapshot(Guid.NewGuid(), PathOf(Root, Department) + "/" + UnitPaths.Id(Guid.NewGuid()), Department, "section", 1,
            categories: ["tennis", "golf"])));

        (await _cache.CategoriesOfAsync(_store.Rows[Department], Ct)).ShouldBe(["football", "golf", "tennis"]);
        (await _cache.CategoriesOfAsync(_store.Rows[Sport], Ct)).ShouldBe(["football", "tennis"]);
    }

    [Fact]
    public void TheHandlerSubscribesToUnitAndMembershipEvents()
    {
        OrgEventsHandler.Patterns.ShouldBe(["org.unit-*.v1", "org.membership-changed.v1"]);
        foreach (var type in new[] { OrgEvents.UnitCreated, OrgEvents.UnitUpdated, OrgEvents.UnitMoved, OrgEvents.UnitRemoved, OrgEvents.MembershipChanged })
            OrgEventsHandler.Patterns.ShouldContain(p => IntegrationTypes.Matches(p, type));
    }
}
