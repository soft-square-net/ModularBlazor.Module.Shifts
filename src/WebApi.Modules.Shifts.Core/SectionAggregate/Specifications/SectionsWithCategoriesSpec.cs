namespace soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate.Specifications;

/// <summary>Every section of the tenant with its categories, by name.</summary>
public sealed class SectionsWithCategoriesSpec : Specification<Section>
{
    public SectionsWithCategoriesSpec() => Query.Include(s => s.Categories).OrderBy(s => s.Name);
}
