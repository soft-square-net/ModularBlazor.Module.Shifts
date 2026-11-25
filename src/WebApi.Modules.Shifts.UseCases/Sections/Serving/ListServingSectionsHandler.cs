using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate.Specifications;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Sections.Serving;

public sealed class ListServingSectionsHandler(IReadRepository<Section> sections)
    : IQueryHandler<ListServingSectionsQuery, Result<IReadOnlyList<SectionDto>>>
{
    public async ValueTask<Result<IReadOnlyList<SectionDto>>> Handle(ListServingSectionsQuery query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.CategoryId)) return Result<IReadOnlyList<SectionDto>>.Invalid(new ValidationError("A category id is required."));
        var categoryId = query.CategoryId.Trim();
        return (await sections.ListAsync(new SectionsServingCategorySpec(categoryId), ct))
            .OrderBy(s => s.IsDepartment) // the category's own sections, then the departments
            .ThenBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(SectionDto.From)
            .ToArray();
    }
}
