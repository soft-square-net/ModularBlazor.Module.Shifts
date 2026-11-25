using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using soft.square.net.WebApi.Contracts.Lookups;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Infrastructure.Data;

namespace soft.square.net.WebApi.Modules.Shifts.Web;

/// <summary>
/// The module's lookups (the host serves them under <c>/api/shifts/lookups/{name}</c>), for pickers and server-side
/// checks in other modules (Content's section of a content, the workflow's routing):
/// <list type="bullet">
/// <item><c>shifts.sections</c>: every section; the column and filter <c>Kind</c> (Category, Department).</item>
/// <item><c>shifts.departments</c>: only the departments, the sections that serve every category.</item>
/// </list>
/// No permission beyond signing in: a section's name and kind are what any editor of the tenant picks from. "Which
/// sections serve category X" (its own sections plus the departments) is <c>GET /api/shifts/sections/serving?category=X</c>;
/// a lookup filters on one entity's own columns, and departments have no category rows to filter on.
/// </summary>
public sealed class ShiftsLookups : ILookupModule
{
    public void Register(ILookupBuilder lookups)
    {
        lookups.For<Section>("sections")
            .Key(s => s.Id)
            .Text(s => s.Name)
            .SearchOn(nameof(Section.Name))
            .FilterOn(nameof(Section.Kind), "Kind")
            .Columns(nameof(Section.Kind))
            .OrderBy(s => s.Name)
            .Label("Sections", "Shift sections: category desks and departments.")
            .Source(Sections);

        lookups.For<Section>("departments")
            .Key(s => s.Id)
            .Text(s => s.Name)
            .SearchOn(nameof(Section.Name))
            .Where(s => s.Kind == SectionKind.Department)
            .OrderBy(s => s.Name)
            .Label("Departments", "Sections that serve every category (Multimedia, SEO).")
            .Source(Sections);
    }

    private static IQueryable<Section> Sections(IServiceProvider services) =>
        services.GetRequiredService<ShiftsDbContext>().Sections.AsNoTracking();
}
