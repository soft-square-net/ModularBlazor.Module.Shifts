namespace soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate.Specifications;

/// <summary>
/// The sections that serve a category: those that list it, and every department. With their categories, by name
/// (the caller puts the category's own sections first).
/// </summary>
public sealed class SectionsServingCategorySpec : Specification<Section>
{
    public SectionsServingCategorySpec(string categoryId) =>
        Query.Where(s => s.Kind == SectionKind.Department || s.Categories.Any(c => c.CategoryId == categoryId))
            .Include(s => s.Categories)
            .OrderBy(s => s.Name);
}
