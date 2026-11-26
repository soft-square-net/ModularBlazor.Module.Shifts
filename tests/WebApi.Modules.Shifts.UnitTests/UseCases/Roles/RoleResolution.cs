using soft.square.net.Organization.Contracts;
using soft.square.net.WebApi.Modules.Shifts.Core.RoleAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Roles;
using static soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases.Units.OrgUnits;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases.Roles;

/// <summary>For each key, the definition nearest to the unit up its path wins; the tenant's holds where no unit overrides it.</summary>
public class RoleResolution
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static ShiftRoleDefinition Desk(Guid? unitId, params string[] permissions) =>
        ShiftRoleDefinition.Create("desk", "Desk", "", permissions, false, unitId);

    [Fact]
    public void TheNearestDefinitionUpThePathWins()
    {
        var tenant = Desk(null, "content.publish");
        var department = Desk(Department, "content.publish", "content.return");
        var sport = Desk(Sport, "content.review");
        var definitions = new[] { sport, tenant, department };

        RoleSet.Resolve(definitions, UnitPaths.Ids(PathOf(Root, Department, Sport))).Find("desk").ShouldBeSameAs(sport);
        RoleSet.Resolve(definitions, UnitPaths.Ids(PathOf(Root, Department, Seo))).Find("desk").ShouldBeSameAs(department);
        RoleSet.Resolve(definitions, UnitPaths.Ids(PathOf(Root, OtherDepartment))).Find("desk").ShouldBeSameAs(tenant);
        RoleSet.Resolve(definitions, []).Find("desk").ShouldBeSameAs(tenant); // a shift with no unit
    }

    [Fact]
    public void AUnitsOwnRoleHoldsOnlyThereAndBelow()
    {
        var photo = ShiftRoleDefinition.Create("photo-editor", "Photo editor", "", ["content.review"], false, Department);

        RoleSet.Resolve([photo], UnitPaths.Ids(PathOf(Root, Department, Sport))).Find("photo-editor").ShouldBeSameAs(photo);
        RoleSet.Resolve([photo], UnitPaths.Ids(PathOf(Root, OtherDepartment))).Find("photo-editor").ShouldBeNull();
        RoleSet.Resolve([photo], []).Find("photo-editor").ShouldBeNull();
    }

    [Fact]
    public async Task ShiftRolesResolvesThroughTheUnitCache()
    {
        var fixture = new ShiftsFixture("admin-1");
        await fixture.Units.ApplyAsync(State(DepartmentUnit()), Ct);
        await fixture.Units.ApplyAsync(State(SportUnit()), Ct);
        fixture.RoleRows.Add(Desk(Department, "content.review"));

        (await fixture.Roles.ForUnitAsync(Sport, Ct)).PermissionsOf("desk").ShouldBe(["content.review"]);
        (await fixture.Roles.ForUnitAsync(null, Ct)).PermissionsOf("desk").ShouldBe(["content.publish", "content.return"]);
        (await fixture.Roles.ForUnitAsync(Guid.NewGuid(), Ct)).PermissionsOf("desk").ShouldBe(["content.publish", "content.return"]); // not cached
        (await fixture.Roles.ForUnitAsync(null, Ct)).PermissionsOf("gone").ShouldBeEmpty();
    }

    [Fact]
    public void KeysAreSlugs()
    {
        ShiftRoleDefinition.Create(" Photo-Editor ", "Photo editor", "", [], false, null).Key.ShouldBe("photo-editor");
        Should.Throw<DomainException>(() => ShiftRoleDefinition.Create("1st", "x", "", [], false, null));
        Should.Throw<DomainException>(() => ShiftRoleDefinition.Create("a b", "x", "", [], false, null));
        Should.Throw<DomainException>(() => ShiftRoleDefinition.Create("desk", " ", "", [], false, null));
        ShiftRoleDefinition.Create("desk", "Desk", "", [], false, null).NameAr.ShouldBe("Desk"); // falls back to the English name
    }

    [Fact]
    public async Task TheDefaultsAreSeededOnceIntoATenantWithNoRoles()
    {
        var repository = Substitute.For<IRepository<ShiftRoleDefinition>>();
        var seed = new SeedShiftRoles(repository);

        (await seed.SeedAsync(Ct)).ShouldBeTrue();
        var added = (IEnumerable<ShiftRoleDefinition>)repository.ReceivedCalls().Single(c => c.GetMethodInfo().Name == nameof(repository.AddRangeAsync)).GetArguments()[0]!;
        added.Select(d => (d.Key, d.IsLead, d.UnitId)).ShouldBe([("manager", true, null), ("desk", false, null), ("editor", false, null)]);

        repository.AnyAsync(Arg.Any<CancellationToken>()).Returns(true);
        (await seed.SeedAsync(Ct)).ShouldBeFalse();
    }

    [Fact]
    public void TheSeededRolesKeepTheOldPermissions()
    {
        var roles = DefaultShiftRoles.Create().ToDictionary(r => r.Key);

        roles["manager"].Permissions.ShouldBe(["content.accept", "content.assign", "content.return", "content.transfer"]);
        roles["desk"].Permissions.ShouldBe(["content.publish", "content.return"]);
        roles["editor"].Permissions.ShouldBe(["content.review"]);
        roles.Values.Count(r => r.IsLead).ShouldBe(1);
    }
}
