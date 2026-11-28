using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate.Specifications;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Sections.List;

public sealed class ListSectionsHandler(IReadRepository<Section> sections, IReadRepository<SectionUnitMap> maps)
    : IQueryHandler<ListSectionsQuery, Result<IReadOnlyList<SectionDto>>>
{
    public async ValueTask<Result<IReadOnlyList<SectionDto>>> Handle(ListSectionsQuery query, CancellationToken ct)
    {
        var moved = (await maps.ListAsync(new SectionUnitMapsSpec(), ct)).ToDictionary(m => m.SectionId, m => (Guid?)m.UnitId);
        return (await sections.ListAsync(new SectionsWithCategoriesSpec(), ct))
            .Select(s => SectionDto.From(s, moved.GetValueOrDefault(s.Id)))
            .ToArray();
    }
}
