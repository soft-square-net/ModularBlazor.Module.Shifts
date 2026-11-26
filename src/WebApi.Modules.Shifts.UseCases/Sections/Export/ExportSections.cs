using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Sections.Move;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Sections.Export;

/// <summary>
/// The sections as JSON, for moving them by hand when this deployment cannot reach the central Organization service:
/// each section with the Section unit to create for it (the same request the move sends). Create the units in
/// Organization, then link them: <c>POST /sections/move-to-organization</c> with <c>links</c>.
/// </summary>
public sealed record ExportSectionsQuery : IQuery<Result<SectionsExport>>;

public sealed record SectionsExport(DateTimeOffset ExportedAt, IReadOnlyList<SectionExportItem> Sections);

/// <summary>
/// One section: its id, name, kind and shift count; the unit to create (<paramref name="Unit"/>: type, names, tags,
/// categories; its parent is the Department you pick); and the unit it moved to, when it has.
/// </summary>
public sealed record SectionExportItem(int SectionId, string Name, SectionKind Kind, IReadOnlyList<string> Categories, int Shifts,
    SectionUnitDraft Unit, Guid? MovedToUnitId);

/// <summary>The Section unit to create in Organization (the body of its create-unit request, without the parent).</summary>
public sealed record SectionUnitDraft(string TypeKey, string NameEn, string NameAr, IReadOnlyList<string> Tags, IReadOnlyList<string> Categories);

public sealed class ExportSectionsHandler(IReadRepository<Section> sections, IReadRepository<SectionUnitMap> maps, IReadRepository<Shift> shifts,
    ShiftCaller caller) : IQueryHandler<ExportSectionsQuery, Result<SectionsExport>>
{
    public async ValueTask<Result<SectionsExport>> Handle(ExportSectionsQuery query, CancellationToken ct)
    {
        var moved = (await maps.ListAsync(new SectionUnitMapsSpec(), ct)).ToDictionary(m => m.SectionId, m => m.UnitId);
        var items = new List<SectionExportItem>();
        foreach (var section in (await sections.ListAsync(new SectionsWithCategoriesSpec(), ct)).OrderBy(s => s.Id))
        {
            var request = MoveSectionsToOrganizationHandler.SectionUnitRequest(section, Guid.Empty);
            items.Add(new SectionExportItem(section.Id, section.Name, section.Kind, [.. section.Categories.Select(c => c.CategoryId)],
                await shifts.CountAsync(new ShiftsOfSectionSpec(section.Id), ct),
                new SectionUnitDraft(request.TypeKey, request.NameEn, request.NameAr, request.Tags ?? [], request.Categories ?? []),
                moved.TryGetValue(section.Id, out var unitId) ? unitId : null));
        }
        return new SectionsExport(caller.Now, items);
    }
}
