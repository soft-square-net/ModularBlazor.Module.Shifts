using Microsoft.EntityFrameworkCore;
using soft.square.net.WebApi.Data;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate.Specifications;

namespace soft.square.net.WebApi.Modules.Shifts.IntegrationTests.Data;

/// <summary>Department sections on a real database: the kind's mapping and "which sections serve category X".</summary>
public sealed class EfRepositoryDepartments : IDisposable
{
    private readonly ShiftsDatabase _database = new();
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _database.Dispose();

    private async Task SeedAsync(string tenant, params Section[] sections)
    {
        await using var context = _database.For(tenant);
        var repository = new EfRepository<Section>(context);
        foreach (var section in sections) await repository.AddAsync(section, Ct);
    }

    [Fact]
    public async Task StoresTheKindByNameAndADepartmentWithoutCategories()
    {
        await SeedAsync("acme", Section.CreateDepartment("Multimedia"), Section.Create("Sport", ["sport"], null, null));

        await using var context = _database.For("acme");
        (await context.Database.SqlQueryRaw<string>("""SELECT "Kind" AS "Value" FROM "sections" ORDER BY "Name" """).ToListAsync(Ct))
            .ShouldBe(["Department", "Category"]); // Multimedia, Sport: by name, as the other enums

        var multimedia = await new EfRepository<Section>(context).FirstOrDefaultAsync(
            new SectionByIdSpec(context.Sections.Single(s => s.Name == "Multimedia").Id), Ct);
        multimedia!.IsDepartment.ShouldBeTrue();
        multimedia.Categories.ShouldBeEmpty();
        multimedia.Scopes.ShouldBe([$"section:{multimedia.Id}"]);
    }

    [Fact]
    public async Task ACategoryIsServedByItsOwnSectionsAndEveryDepartment()
    {
        await SeedAsync("acme",
            Section.Create("Sport", ["sport", "football"], null, null),
            Section.Create("Politics", ["politics"], null, null),
            Section.CreateDepartment("Multimedia"),
            Section.CreateDepartment("SEO"));

        await using var context = _database.For("acme");
        var sections = new EfRepository<Section>(context);

        (await sections.ListAsync(new SectionsServingCategorySpec("football"), Ct)).Select(s => s.Name)
            .ShouldBe(["Multimedia", "SEO", "Sport"]);
        (await sections.ListAsync(new SectionsServingCategorySpec("politics"), Ct)).Select(s => s.Name)
            .ShouldBe(["Multimedia", "Politics", "SEO"]);
        (await sections.ListAsync(new SectionsServingCategorySpec("weather"), Ct)).Select(s => s.Name)
            .ShouldBe(["Multimedia", "SEO"]); // a category no desk lists: only the departments
    }

    [Fact]
    public async Task AnotherTenantsDepartmentsDoNotServe()
    {
        await SeedAsync("acme", Section.CreateDepartment("Multimedia"));
        await SeedAsync("globex", Section.Create("Sport", ["sport"], null, null));

        await using var context = _database.For("globex");
        (await new EfRepository<Section>(context).ListAsync(new SectionsServingCategorySpec("sport"), Ct)).Select(s => s.Name)
            .ShouldBe(["Sport"]);
    }

    [Fact]
    public async Task TurningASectionIntoADepartmentDropsItsCategories()
    {
        await SeedAsync("acme", Section.Create("Desk", ["politics", "economy"], null, null));

        await using (var context = _database.For("acme"))
        {
            var sections = new EfRepository<Section>(context);
            var desk = (await sections.ListAsync(new SectionsWithCategoriesSpec(), Ct)).Single();
            desk.Update("Multimedia", [], null, null, SectionKind.Department);
            await sections.UpdateAsync(desk, Ct);
        }

        await using var reader = _database.For("acme");
        // The removed rows lose their section (the relationship is optional, so EF orphans them rather than deleting
        // them, as for any category change) and so serve nothing.
        var reloaded = (await new EfRepository<Section>(reader).ListAsync(new SectionsWithCategoriesSpec(), Ct)).Single();
        reloaded.IsDepartment.ShouldBeTrue();
        reloaded.Categories.ShouldBeEmpty();
        (await new EfRepository<Section>(reader).ListAsync(new SectionsServingCategorySpec("sport"), Ct)).Select(s => s.Name)
            .ShouldBe(["Multimedia"]);
    }
}
