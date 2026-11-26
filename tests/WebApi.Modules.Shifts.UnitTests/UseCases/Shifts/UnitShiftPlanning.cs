using soft.square.net.Organization.Contracts;
using soft.square.net.WebApi.Modules.Shifts.Core.RoleAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.UnitTests.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Add;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.CheckOut;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Publish;
using static soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases.Units.OrgUnits;

namespace soft.square.net.WebApi.Modules.Shifts.UnitTests.UseCases.Shifts;

/// <summary>
/// Planning a unit's shift: manager rights come down the tree (shifts.manage on the unit's scopes), the rota's people
/// come from the unit or a unit above it (D6) and its roles from the definitions that hold for the unit, and the grants
/// follow those definitions.
/// </summary>
public class UnitShiftPlanning
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly ShiftsFixture _fixture = new("dept-manager");
    private readonly AddShiftHandler _add;

    public UnitShiftPlanning()
    {
        _fixture.SectionReader.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(_fixture.Section);
        _fixture.Shifts.AddAsync(Arg.Any<Shift>(), Arg.Any<CancellationToken>()).Returns(c => c.Arg<Shift>());
        _add = new AddShiftHandler(_fixture.Shifts, _fixture.SectionReader, _fixture.ShiftUnits, _fixture.Rota, _fixture.Authority, _fixture.Details);
        foreach (var unit in new[] { RootUnit(), DepartmentUnit(), SportUnit(), SeoUnit(), Snapshot(OtherDepartment, PathOf(Root, OtherDepartment), Root, "department", 1) })
            _fixture.Units.ApplyAsync(State(unit), Ct).GetAwaiter().GetResult();
    }

    private Task<Result<ShiftDto>> Add(Guid? unitId, params RotaEntry[] rota) =>
        _add.Handle(new AddShiftCommand(1, _fixture.Clock.Now.AddHours(1), _fixture.Clock.Now.AddHours(9), rota, unitId), Ct).AsTask();

    [Fact]
    public async Task ADepartmentManagerPlansTheShiftsOfItsSectionsOnly()
    {
        _fixture.Manages(Department);
        _fixture.Member(Department, "m");
        _fixture.Member(OtherDepartment, "m");

        (await Add(Sport, new RotaEntry("m", "manager"))).IsSuccess.ShouldBeTrue();
        (await Add(Department, new RotaEntry("m", "manager"))).IsSuccess.ShouldBeTrue();
        (await Add(OtherDepartment, new RotaEntry("m", "manager"))).Status.ShouldBe(ResultStatus.Forbidden);
        (await Add(null, new RotaEntry("m", "manager"))).Status.ShouldBe(ResultStatus.Forbidden); // no unit: tenant-wide only
    }

    [Fact]
    public async Task TheRightsAreCheckedOnTheUnitAndItsAncestors()
    {
        _fixture.Manages(Department);
        _fixture.Member(Sport, "m");

        await Add(Sport, new RotaEntry("m", "manager"));

        await _fixture.Access.Received().HasPermissionAsync(ShiftPermissions.Manage,
            Arg.Is<IReadOnlyCollection<string>>(s => s.SequenceEqual(UnitScopes.For(PathOf(Root, Department, Sport)))), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TheRotaComesFromTheUnitOrAUnitAboveIt()
    {
        _fixture.ShiftAdmin();
        _fixture.Member(Root, "editor-from-root");
        _fixture.Member(Sport, "desk-from-sport");
        _fixture.Member(Seo, "seo-only");

        var ok = await Add(Sport, new RotaEntry("desk-from-sport", "manager"), new RotaEntry("editor-from-root", "editor"));
        var outsider = await Add(Sport, new RotaEntry("desk-from-sport", "manager"), new RotaEntry("seo-only", "desk"));

        ok.IsSuccess.ShouldBeTrue();
        outsider.Status.ShouldBe(ResultStatus.Invalid);
        outsider.ValidationErrors.Single().ErrorMessage.ShouldContain("seo-only");
    }

    [Fact]
    public async Task AnEndedMembershipOrOneWithoutAnIdentityDoesNotCount()
    {
        _fixture.ShiftAdmin();
        _fixture.Member(Sport, "left", to: _fixture.Clock.Now);
        _fixture.Member(Sport, "joins-later", from: _fixture.Clock.Now.AddDays(1));
        _fixture.Member(Sport, "");

        (await Add(Sport, new RotaEntry("left", "manager"))).Status.ShouldBe(ResultStatus.Invalid);
        (await Add(Sport, new RotaEntry("joins-later", "manager"))).Status.ShouldBe(ResultStatus.Invalid);
        (await Add(Sport, new RotaEntry("", "manager"), new RotaEntry("nobody", "desk"))).Status.ShouldBe(ResultStatus.Invalid);
    }

    [Fact]
    public async Task WithoutOrganizationTheRotaCannotBeChecked()
    {
        _fixture.ShiftAdmin();
        _fixture.Organization.Configured.Returns(false);

        (await Add(Sport, new RotaEntry("m", "manager"))).Status.ShouldBe(ResultStatus.Unavailable);

        _fixture.Organization.Configured.Returns(true);
        _fixture.Directory.MembersOfAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns<IReadOnlyList<DirectoryMemberDto>>(_ => throw new HttpRequestException("down"));
        (await Add(Sport, new RotaEntry("m", "manager"))).Status.ShouldBe(ResultStatus.Unavailable);
    }

    [Fact]
    public async Task AShiftWithNoUnitSkipsTheMembershipCheck()
    {
        _fixture.ShiftAdmin();

        var result = await Add(null, new RotaEntry("anyone", "Manager")); // the old role names still work

        result.IsSuccess.ShouldBeTrue();
        result.Value.Rota.Single().ShouldBe(new RotaMemberDto("anyone", "manager", true, "expected", null, null, null, null, false, null, null, null));
    }

    [Fact]
    public async Task UnknownRolesAndASecondLeadAreRefused()
    {
        _fixture.ShiftAdmin();
        _fixture.Member(Sport, "a");
        _fixture.Member(Sport, "b");

        (await Add(Sport, new RotaEntry("a", "pilot"))).Status.ShouldBe(ResultStatus.Invalid);
        await Should.ThrowAsync<DomainException>(() => Add(Sport, new RotaEntry("a", "manager"), new RotaEntry("b", "manager")));
    }

    [Fact]
    public async Task AUnitsOwnRoleIsUsableThereAndGrantsItsPermissions()
    {
        _fixture.ShiftAdmin();
        _fixture.Member(Department, "m");
        _fixture.Member(Department, "p");
        _fixture.RoleRows.Add(ShiftRoleDefinition.Create("photo-editor", "Photo editor", "", ["content.review", "storage.upload"], false, Department));
        _fixture.RoleRows.Add(ShiftRoleDefinition.Create("manager", "Section lead", "", ["content.accept"], true, Sport));

        var planned = await Add(Sport, new RotaEntry("m", "manager"), new RotaEntry("p", "photo-editor"));
        (await Add(OtherDepartment, new RotaEntry("p", "photo-editor"))).Status.ShouldBe(ResultStatus.Invalid);

        var shift = _fixture.Shifts.ReceivedCalls().Select(c => c.GetArguments()[0]).OfType<Shift>().First();
        _fixture.Stored(shift);
        (await new PublishShiftHandler(_fixture.Changes, _fixture.Authority, _fixture.Notices).Handle(new PublishShiftCommand(shift.Id), Ct)).IsSuccess.ShouldBeTrue();
        ShiftBuilder.CheckEveryoneIn(shift, shift.StartsAt);
        await _fixture.ShiftGrants.WriteAsync(shift, _fixture.Section, Ct);

        planned.IsSuccess.ShouldBeTrue();
        var grants = _fixture.WrittenGrants(shift);
        grants.Where(g => g.UserId == "m").Select(g => g.Permission).Distinct().ShouldBe(["content.accept"]); // Sport's own manager
        grants.Where(g => g.UserId == "p").Select(g => g.Permission).Distinct().ShouldBe(["content.review", "storage.upload"], ignoreOrder: true);
        grants.Select(g => g.Scope).ShouldContain($"unit:{UnitPaths.Id(Sport)}");
    }

    [Fact]
    public async Task ADraftCannotBePublishedWithoutALead()
    {
        _fixture.ShiftAdmin();
        _fixture.Member(Sport, "d");
        var draft = (await Add(Sport, new RotaEntry("d", "desk"))).Value;
        var shift = _fixture.Shifts.ReceivedCalls().Select(c => c.GetArguments()[0]).OfType<Shift>().First();
        _fixture.Stored(shift);

        draft.Published.ShouldBeFalse();
        await Should.ThrowAsync<DomainException>(() => new PublishShiftHandler(_fixture.Changes, _fixture.Authority, _fixture.Notices)
            .Handle(new PublishShiftCommand(shift.Id), Ct).AsTask());
    }

    [Fact]
    public async Task ADepartmentManagerChecksOutASectionsShift()
    {
        var fixture = new ShiftsFixture("dept-manager");
        await fixture.Units.ApplyAsync(State(DepartmentUnit()), Ct);
        await fixture.Units.ApplyAsync(State(SportUnit()), Ct);
        fixture.Manages(Department);
        var shift = ShiftBuilder.Published(fixture.Clock.Now.AddHours(-9), Sport);
        fixture.Stored(shift);

        var result = await new CheckOutShiftHandler(fixture.Changes, fixture.Caller, fixture.Authority, fixture.Notices).Handle(new CheckOutShiftCommand(shift.Id), Ct);

        result.IsSuccess.ShouldBeTrue();
    }
}
