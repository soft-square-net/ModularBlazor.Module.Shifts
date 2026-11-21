using soft.square.net.WebApi.Data;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate.Specifications;

namespace soft.square.net.WebApi.Modules.Shifts.IntegrationTests.Data;

/// <summary>The module's mappings and specifications through the platform's repository, on a real database.</summary>
public sealed class EfRepositoryShifts : IDisposable
{
    private static readonly DateTimeOffset Start = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);
    private readonly ShiftsDatabase _database = new();
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _database.Dispose();

    private async Task<(Section Section, Shift Shift)> SeedAsync(string tenant = "acme", DateTimeOffset? startsAt = null)
    {
        await using var context = _database.For(tenant);
        var section = await new EfRepository<Section>(context).AddAsync(Section.Create("Politics desk", ["politics", "economy"], 3, null), Ct);
        var shift = Shift.Create(section.Id, startsAt ?? Start, (startsAt ?? Start).AddHours(8),
            [("manager-1", ShiftRole.Manager), ("desk-1", ShiftRole.Desk), ("desk-1", ShiftRole.Editor)]);
        await new EfRepository<Shift>(context).AddAsync(shift, Ct);
        return (section, shift);
    }

    [Fact]
    public async Task StoresASectionWithItsCategoriesAndTenant()
    {
        var (saved, _) = await SeedAsync();

        await using var context = _database.For("acme");
        var section = await new EfRepository<Section>(context).FirstOrDefaultAsync(new SectionByIdSpec(saved.Id), Ct);

        section.ShouldNotBeNull();
        section.TenantId.ShouldBe("acme");
        section.AutoAssignDelayMinutes.ShouldBe(3);
        section.Categories.Select(c => c.CategoryId).ShouldBe(["politics", "economy"], ignoreOrder: true);
    }

    [Fact]
    public async Task StoresAShiftWithItsRotaAndRolesByName()
    {
        var (_, saved) = await SeedAsync();

        await using var context = _database.For("acme");
        var shift = await new EfRepository<Shift>(context).FirstOrDefaultAsync(new ShiftByIdSpec(saved.Id), Ct);

        shift.ShouldNotBeNull();
        shift.GrantSource.ShouldBe($"shift:{saved.Id}");
        shift.Members.Select(m => (m.UserId, m.Role)).ShouldBe(
            [("manager-1", ShiftRole.Manager), ("desk-1", ShiftRole.Desk), ("desk-1", ShiftRole.Editor)], ignoreOrder: true);
    }

    [Fact]
    public async Task AnotherTenantDoesNotSeeIt()
    {
        var (_, saved) = await SeedAsync();

        await using var other = _database.For("globex");
        (await new EfRepository<Shift>(other).FirstOrDefaultAsync(new ShiftByIdSpec(saved.Id), Ct)).ShouldBeNull();

        await using var none = _database.For(null);
        (await new EfRepository<Section>(none).CountAsync(Ct)).ShouldBe(0);
    }

    [Fact]
    public async Task ARotaChangeAndACheckOutAreSaved()
    {
        var (_, saved) = await SeedAsync();

        await using (var context = _database.For("acme"))
        {
            var shifts = new EfRepository<Shift>(context);
            var shift = await shifts.FirstOrDefaultAsync(new ShiftByIdSpec(saved.Id), Ct);
            shift!.SetRota([("manager-1", ShiftRole.Manager), ("editor-2", ShiftRole.Editor)]);
            shift.CheckOutMember("editor-2", Start.AddHours(2));
            await shifts.UpdateAsync(shift, Ct);
        }

        await using var reader = _database.For("acme");
        var reloaded = await new EfRepository<Shift>(reader).FirstOrDefaultAsync(new ShiftByIdSpec(saved.Id), Ct);
        reloaded!.Members.Select(m => m.UserId).ShouldBe(["manager-1", "editor-2"], ignoreOrder: true);
        reloaded.Members.Single(m => m.UserId == "editor-2").CheckedOutAt.ShouldBe(Start.AddHours(2));
    }

    [Fact]
    public async Task TheWindowAndMemberSpecsFindOverlappingShifts()
    {
        var (section, _) = await SeedAsync();
        await SeedAsync(startsAt: Start.AddDays(3));

        await using var context = _database.For("acme");
        var shifts = new EfRepository<Shift>(context);

        (await shifts.ListAsync(new ShiftsInWindowSpec(Start.AddHours(7), Start.AddHours(9)), Ct)).Count.ShouldBe(1);
        (await shifts.ListAsync(new ShiftsInWindowSpec(Start.AddHours(-1), sectionId: section.Id), Ct)).Count.ShouldBe(1);
        (await shifts.ListAsync(new MemberShiftsSpec("desk-1", Start, Start.AddDays(7)), Ct)).Count.ShouldBe(2);
        (await shifts.ListAsync(new MemberShiftsSpec("nobody", Start, Start.AddDays(7)), Ct)).ShouldBeEmpty();
    }

    [Fact]
    public async Task OverdueMeansPublishedPastItsEndAndNotCheckedOut()
    {
        var (_, saved) = await SeedAsync();
        await using var context = _database.For("acme");
        var shifts = new EfRepository<Shift>(context);
        var afterEnd = Start.AddHours(9);

        (await shifts.ListAsync(new OverdueShiftsSpec(afterEnd), Ct)).ShouldBeEmpty(); // not published

        var shift = await shifts.FirstOrDefaultAsync(new ShiftByIdSpec(saved.Id), Ct);
        shift!.Publish();
        await shifts.UpdateAsync(shift, Ct);
        (await shifts.ListAsync(new OverdueShiftsSpec(afterEnd), Ct)).Select(s => s.Id).ShouldBe([saved.Id]);
        (await shifts.ListAsync(new OverdueShiftsSpec(Start.AddHours(7)), Ct)).ShouldBeEmpty(); // still running

        shift.CheckOut("workflow", afterEnd);
        await shifts.UpdateAsync(shift, Ct);
        (await shifts.ListAsync(new OverdueShiftsSpec(afterEnd), Ct)).ShouldBeEmpty();
    }
}
