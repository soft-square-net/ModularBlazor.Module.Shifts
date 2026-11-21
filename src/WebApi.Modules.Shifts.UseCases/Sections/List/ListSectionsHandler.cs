using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate.Specifications;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Sections.List;

public sealed class ListSectionsHandler(IReadRepository<Section> sections) : IQueryHandler<ListSectionsQuery, Result<IReadOnlyList<SectionDto>>>
{
    public async ValueTask<Result<IReadOnlyList<SectionDto>>> Handle(ListSectionsQuery query, CancellationToken ct) =>
        (await sections.ListAsync(new SectionsWithCategoriesSpec(), ct)).Select(SectionDto.From).ToArray();
}
