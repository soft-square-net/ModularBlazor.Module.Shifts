using soft.square.net.WebApi.Data;
using soft.square.net.WebApi.Modules.Shifts.Core.RoleAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.RoleAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.TemplateAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.TemplateAggregate.Specifications;

namespace soft.square.net.WebApi.Modules.Shifts.IntegrationTests.Data;

/// <summary>Role definitions and shift templates through the platform's repository, on a real database.</summary>
public sealed class EfRepositoryRolesAndTemplates : IDisposable
{
    private readonly ShiftsDatabase _database = new();
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task StoresARoleDefinitionWithItsPermissionsAndUnit()
    {
        var unit = Guid.NewGuid();
        await using (var context = _database.For("acme"))
            await new EfRepository<ShiftRoleDefinition>(context).AddRangeAsync([
                ShiftRoleDefinition.Create("desk", "Desk", "الديسك", ["content.publish", "content.return"], false, null),
                ShiftRoleDefinition.Create("desk", "Sport desk", "", ["content.review"], false, unit)
            ], Ct);

        await using var read = _database.For("acme");
        var roles = await new EfRepository<ShiftRoleDefinition>(read).ListAsync(new AllRoleDefinitionsSpec(), Ct);

        roles.Select(r => (r.Key, r.NameAr, r.UnitId, string.Join(",", r.Permissions))).ShouldBe(
            [("desk", "الديسك", (Guid?)null, "content.publish,content.return"), ("desk", "Sport desk", unit, "content.review")], ignoreOrder: true);
        (await new EfRepository<ShiftRoleDefinition>(_database.For("other")).ListAsync(new AllRoleDefinitionsSpec(), Ct)).ShouldBeEmpty();
    }

    [Fact]
    public async Task StoresATemplateWithItsTimesSlotsAndMembers()
    {
        var unit = Guid.NewGuid();
        var template = ShiftTemplate.Create(unit, "Night", new TimeOnly(22, 30), new TimeSpan(9, 15, 0), [("manager", 1), ("desk", 2)],
            [("u1", "manager"), ("u2", "desk")]);
        await using (var context = _database.For("acme"))
            await new EfRepository<ShiftTemplate>(context).AddAsync(template, Ct);

        await using var read = _database.For("acme");
        var saved = await new EfRepository<ShiftTemplate>(read).FirstOrDefaultAsync(new TemplateByIdSpec(template.Id), Ct);

        saved.ShouldNotBeNull();
        saved.UnitId.ShouldBe(unit);
        saved.StartLocal.ShouldBe(new TimeOnly(22, 30));
        saved.Duration.ShouldBe(new TimeSpan(9, 15, 0));
        saved.Slots.Select(s => (s.RoleKey, s.Count)).ShouldBe([("manager", 1), ("desk", 2)], ignoreOrder: true);
        saved.DefaultMembers.Select(m => (m.UserId, m.RoleKey)).ShouldBe([("u1", "manager"), ("u2", "desk")], ignoreOrder: true);
        (await new EfRepository<ShiftTemplate>(read).ListAsync(new TemplatesSpec(Guid.NewGuid()), Ct)).ShouldBeEmpty();
    }

    [Fact]
    public async Task StoresAUnitShiftWithNoSection()
    {
        var unit = Guid.NewGuid();
        var start = new DateTimeOffset(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);
        var shift = Shift.Create(null, start, start.AddHours(8), [new("u1", "desk", false)], unit);
        await using (var context = _database.For("acme"))
            await new EfRepository<Shift>(context).AddAsync(shift, Ct);

        await using var read = _database.For("acme");
        var saved = await new EfRepository<Shift>(read).GetByIdAsync(shift.Id, Ct);

        saved.ShouldNotBeNull();
        saved.SectionId.ShouldBeNull();
        saved.UnitId.ShouldBe(unit);
    }
}
