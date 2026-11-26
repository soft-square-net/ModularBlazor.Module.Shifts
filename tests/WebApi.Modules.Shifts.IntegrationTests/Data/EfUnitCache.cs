using Microsoft.EntityFrameworkCore;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.UnitAggregate;
using soft.square.net.WebApi.Modules.Shifts.Infrastructure.Data;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Units;

namespace soft.square.net.WebApi.Modules.Shifts.IntegrationTests.Data;

/// <summary>The unit cache and the section-to-unit map against a real database: the list columns, paths and versions.</summary>
public sealed class EfUnitCache : IDisposable
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static readonly Guid Org = Guid.NewGuid();
    private static readonly Guid Dept = Guid.Parse("0d000000-0000-0000-0000-000000000001");
    private static readonly Guid Sport = Guid.Parse("05000000-0000-0000-0000-000000000001");
    private static readonly Guid Seo = Guid.Parse("05000000-0000-0000-0000-000000000002");
    private static readonly string DeptPath = "/" + Dept.ToString("N");

    private readonly ShiftsDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static UnitState State(Guid id, string path, int version, string name, IReadOnlyList<string> tags, IReadOnlyList<string> categories) =>
        new(id, Org, id == Dept ? null : Dept, id == Dept ? "department" : "section", path, name, name + " ar", tags, "Africa/Cairo", categories, version);

    private async Task Apply(params UnitState[] states)
    {
        await using var db = _database.For("acme");
        var cache = new UnitCache(new EfUnitCacheStore(db));
        foreach (var state in states) await cache.ApplyAsync(state, Ct);
        await cache.SaveAsync(Ct);
    }

    [Fact]
    public async Task TagsAndCategoriesRoundTripAndAChangeToThemIsSaved()
    {
        await Apply(
            State(Dept, DeptPath, 1, "News", [], []),
            State(Seo, DeptPath + "/" + Seo.ToString("N"), 1, "SEO", ["department"], ["*"]),
            State(Sport, DeptPath + "/" + Sport.ToString("N"), 1, "Sport", [], ["football", "tennis"]));
        await Apply(State(Sport, DeptPath + "/" + Sport.ToString("N"), 2, "Sport", ["night"], ["football"]));

        await using var db = _database.For("acme");
        var units = await db.Units.AsNoTracking().ToDictionaryAsync(u => u.Id, Ct);
        units[Seo].Tags.ShouldBe(["department"]);
        units[Seo].Categories.ShouldBe(["*"]);
        units[Seo].IsDepartmentSection.ShouldBeTrue();
        units[Sport].Tags.ShouldBe(["night"]);
        units[Sport].Categories.ShouldBe(["football"]);
        units[Sport].Version.ShouldBe(2);
        units.Values.ShouldAllBe(u => u.TenantId == "acme");

        var cache = new UnitCache(new EfUnitCacheStore(db));
        (await cache.CategoriesOfAsync(units[Dept], Ct)).ShouldBe(["*", "football"]);
    }

    [Fact]
    public async Task AMoveRebasesTheSavedSubtree()
    {
        var sportPath = DeptPath + "/" + Sport.ToString("N");
        await Apply(State(Dept, DeptPath, 1, "News", [], []), State(Sport, sportPath, 1, "Sport", [], ["football"]));
        var newDeptPath = "/" + Guid.NewGuid().ToString("N") + DeptPath;

        await using (var db = _database.For("acme"))
        {
            var cache = new UnitCache(new EfUnitCacheStore(db));
            (await cache.MoveAsync(new(Dept, Org, null, Guid.NewGuid(), DeptPath, newDeptPath, 2), Ct)).ShouldBeTrue();
            await cache.SaveAsync(Ct);
        }

        await using var read = _database.For("acme");
        (await read.Units.SingleAsync(u => u.Id == Sport, Ct)).Path.ShouldBe(newDeptPath + "/" + Sport.ToString("N"));
    }

    [Fact]
    public async Task ASectionMovesOnce()
    {
        await using (var db = _database.For("acme"))
        {
            db.SectionUnitMaps.Add(new SectionUnitMap(1, Sport, DateTimeOffset.UtcNow, "admin"));
            await db.SaveChangesAsync(Ct);
        }

        await using var again = _database.For("acme");
        again.SectionUnitMaps.Add(new SectionUnitMap(1, Seo, DateTimeOffset.UtcNow, "admin"));
        await Should.ThrowAsync<DbUpdateException>(() => again.SaveChangesAsync(Ct));
    }
}
