using Ardalis.Specification;
using soft.square.net.WebApi.Modules.Shifts.Core.RoleAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.TemplateAggregate;
using soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Roles;
using static soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases.Units.OrgUnits;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases.Roles;

/// <summary>Role definitions: who may add and change them, the permission catalog, and what a change does to shifts.</summary>
public class RoleDefinitionHandlersHandle
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly ShiftsFixture _fixture = new("admin-1");
    private readonly IRepository<ShiftRoleDefinition> _roles = Substitute.For<IRepository<ShiftRoleDefinition>>();
    private readonly IReadRepository<ShiftTemplate> _templates = Substitute.For<IReadRepository<ShiftTemplate>>();
    private readonly IPermissionCatalog _catalog = Substitute.For<IPermissionCatalog>();
    private readonly List<Shift> _notEnded = [];
    private readonly RoleUsage _usage;
    private readonly RoleGrants _grants;
    private readonly RolePermissions _permissions;

    public RoleDefinitionHandlersHandle()
    {
        for (var i = 0; i < _fixture.RoleRows.Count; i++) _fixture.RoleRows[i].WithId(i + 1);
        _fixture.Section.WithId(1); // the shifts' section, as RoleGrants looks it up
        foreach (var unit in new[] { RootUnit(), DepartmentUnit(), SportUnit() }) _fixture.Units.ApplyAsync(State(unit), Ct).GetAwaiter().GetResult();
        _roles.ListAsync(Arg.Any<ISpecification<ShiftRoleDefinition>>(), Arg.Any<CancellationToken>()).Returns(_ => _fixture.RoleRows.ToList());
        _roles.AddAsync(Arg.Any<ShiftRoleDefinition>(), Arg.Any<CancellationToken>()).Returns(c => { _fixture.RoleRows.Add(c.Arg<ShiftRoleDefinition>()); return c.Arg<ShiftRoleDefinition>(); });
        _roles.DeleteAsync(Arg.Any<ShiftRoleDefinition>(), Arg.Any<CancellationToken>()).Returns(c => { _fixture.RoleRows.Remove(c.Arg<ShiftRoleDefinition>()); return 1; });
        _fixture.ShiftReader.ListAsync(Arg.Any<ISpecification<Shift>>(), Arg.Any<CancellationToken>()).Returns(c => c.Arg<ISpecification<Shift>>().Evaluate(_notEnded).ToList());
        _templates.ListAsync(Arg.Any<ISpecification<ShiftTemplate>>(), Arg.Any<CancellationToken>()).Returns([]);
        _catalog.ListAsync(Arg.Any<CancellationToken>()).Returns([
            new ModulePermissionSet("content", "Content", [new("content.accept"), new("content.review"), new("content.publish"), new("content.return")]),
            new ModulePermissionSet("storage", "Storage", [new("storage.upload")])
        ]);
        _usage = new RoleUsage(_fixture.RoleReader, _fixture.ShiftReader, _templates, _fixture.Units, _fixture.Caller);
        _grants = new RoleGrants(_fixture.SectionReader, _fixture.ShiftGrants);
        _permissions = new RolePermissions(_catalog);
    }

    private AddRoleDefinitionHandler Add => new(_roles, _fixture.Units, _fixture.Authority, _permissions);
    private UpdateRoleDefinitionHandler Update => new(_roles, _usage, _fixture.Authority, _permissions, _grants);
    private DeleteRoleDefinitionHandler Delete => new(_roles, _usage, _fixture.Authority, _grants);

    private ShiftRoleDefinition Row(string key, Guid? unitId = null) => _fixture.RoleRows.Single(r => r.Key == key && r.UnitId == unitId);

    private ShiftRoleDefinition Override(int id, string key, bool isLead, Guid unitId, params string[] permissions)
    {
        var definition = ShiftRoleDefinition.Create(key, key, "", permissions, isLead, unitId).WithId(id);
        _fixture.RoleRows.Add(definition);
        return definition;
    }

    [Fact]
    public async Task ATenantWideRoleNeedsATenantWideManager()
    {
        _fixture.Manages(Department);

        var tenantWide = await Add.Handle(new AddRoleDefinitionCommand("photo", "Photo", null, ["content.review"], false, null), Ct);
        var forDepartment = await Add.Handle(new AddRoleDefinitionCommand("photo", "Photo", null, ["content.review"], false, Department), Ct);
        var forSport = await Add.Handle(new AddRoleDefinitionCommand("desk", "Sport desk", "ديسك الرياضة", ["content.publish"], false, Sport), Ct);

        tenantWide.Status.ShouldBe(ResultStatus.Forbidden);
        forDepartment.IsSuccess.ShouldBeTrue();
        forSport.Value.Key.ShouldBe("desk");
        forSport.Value.NameAr.ShouldBe("ديسك الرياضة");
        forSport.Value.UnitId.ShouldBe(Sport);
    }

    [Fact]
    public async Task PermissionsComeFromTheCatalogAndKeysAreUniquePerUnit()
    {
        _fixture.ShiftAdmin();

        (await Add.Handle(new AddRoleDefinitionCommand("pilot", "Pilot", null, ["plane.fly"], false, null), Ct)).Status.ShouldBe(ResultStatus.Invalid);
        (await Add.Handle(new AddRoleDefinitionCommand("desk", "Desk", null, [], false, null), Ct)).Status.ShouldBe(ResultStatus.Conflict);
        (await Add.Handle(new AddRoleDefinitionCommand("Bad Key", "x", null, [], false, null), Ct)).Status.ShouldBe(ResultStatus.Invalid);
        (await Add.Handle(new AddRoleDefinitionCommand("desk", "x", null, [], false, Guid.NewGuid()), Ct)).Status.ShouldBe(ResultStatus.Invalid); // unknown unit
        (await Add.Handle(new AddRoleDefinitionCommand("desk", "Sport desk", null, [], false, Sport), Ct)).IsSuccess.ShouldBeTrue(); // same key, other unit
        (await Add.Handle(new AddRoleDefinitionCommand("admin", "Grants admin", null, [PlatformPermissions.GrantsManage], false, null), Ct))
            .IsSuccess.ShouldBeTrue(); // the platform's own permissions count too
    }

    [Fact]
    public async Task NewPermissionsRewriteTheGrantsOfPublishedShiftsThatHaveNotEnded()
    {
        _fixture.ShiftAdmin();
        var running = ShiftBuilder.CheckedIn(_fixture.Clock.Now.AddHours(-1)).WithId(10);
        var ended = ShiftBuilder.Published(_fixture.Clock.Now.AddHours(-10)).WithId(11);
        _notEnded.AddRange([running, ended]);

        var result = await Update.Handle(new UpdateRoleDefinitionCommand(Row("editor").Id, "Editor", null, ["content.review", "content.return"], false), Ct);

        result.IsSuccess.ShouldBeTrue();
        _fixture.WrittenGrants(running).Where(g => g.UserId == ShiftBuilder.Editor).Select(g => g.Permission).Distinct()
            .ShouldBe(["content.return", "content.review"], ignoreOrder: true);
        _fixture.Grants.ReceivedCalls().ShouldNotContain(c => c.GetArguments().Length > 0 && Equals(c.GetArguments()[0], ended.GrantSource));
    }

    [Fact]
    public async Task TheLeadFlagCannotChangeWhileARoleIsInUse()
    {
        _fixture.ShiftAdmin();
        _notEnded.Add(ShiftBuilder.Planned(_fixture.Clock.Now.AddHours(1)).WithId(10));

        var result = await Update.Handle(new UpdateRoleDefinitionCommand(Row("desk").Id, "Desk", null, ["content.publish"], true), Ct);

        result.Status.ShouldBe(ResultStatus.Conflict);
        Row("desk").IsLead.ShouldBeFalse();
    }

    [Fact]
    public async Task ARoleInUseIsDeletedOnlyWhenAnotherTakesOver()
    {
        _fixture.ShiftAdmin();
        Override(20, "desk", false, Sport, "content.review");
        var leadDesk = Override(21, "desk", true, Department, "content.review");
        _notEnded.Add(ShiftBuilder.Published(_fixture.Clock.Now.AddHours(1), Sport).WithId(10));

        // The tenant's desk is not in force for the Sport shift (Sport's own is): it goes.
        (await Delete.Handle(new DeleteRoleDefinitionCommand(Row("desk").Id), Ct)).IsSuccess.ShouldBeTrue();
        // Sport's desk would hand over to the Department's, which is a lead: refused.
        (await Delete.Handle(new DeleteRoleDefinitionCommand(20), Ct)).Status.ShouldBe(ResultStatus.Conflict);
        _fixture.RoleRows.ShouldContain(r => r.Id == 20);

        _fixture.RoleRows.Remove(leadDesk);
        // Nothing left to take over: refused.
        (await Delete.Handle(new DeleteRoleDefinitionCommand(20), Ct)).Status.ShouldBe(ResultStatus.Conflict);
    }

    [Fact]
    public async Task DeletingAnOverrideHandsItsShiftsBackToTheTenantsRole()
    {
        _fixture.ShiftAdmin();
        Override(20, "desk", false, Sport, "content.review");
        var shift = ShiftBuilder.CheckedIn(_fixture.Clock.Now.AddHours(1), Sport).WithId(10);
        _notEnded.Add(shift);

        var deleted = await Delete.Handle(new DeleteRoleDefinitionCommand(20), Ct);

        deleted.IsSuccess.ShouldBeTrue();
        _fixture.WrittenGrants(shift).Where(g => g.UserId == ShiftBuilder.Desk).Select(g => g.Permission).Distinct()
            .ShouldBe(["content.publish", "content.return"], ignoreOrder: true);
    }

    [Fact]
    public async Task AUnitManagerChangesOnlyItsUnitsRoles()
    {
        _fixture.Manages(Department);
        Override(20, "desk", false, Sport, "content.review");

        (await Update.Handle(new UpdateRoleDefinitionCommand(Row("desk").Id, "Desk", null, [], false), Ct)).Status.ShouldBe(ResultStatus.Forbidden);
        (await Update.Handle(new UpdateRoleDefinitionCommand(20, "Sport desk", null, ["content.publish"], false), Ct)).IsSuccess.ShouldBeTrue();
        (await Delete.Handle(new DeleteRoleDefinitionCommand(Row("manager").Id), Ct)).Status.ShouldBe(ResultStatus.Forbidden);
        (await Delete.Handle(new DeleteRoleDefinitionCommand(99), Ct)).Status.ShouldBe(ResultStatus.NotFound);
    }
}
