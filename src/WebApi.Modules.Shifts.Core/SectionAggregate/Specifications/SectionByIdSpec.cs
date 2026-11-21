namespace soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate.Specifications;

/// <summary>The section with this id and its categories.</summary>
public sealed class SectionByIdSpec : SingleResultSpecification<Section>
{
    public SectionByIdSpec(int id) => Query.Where(s => s.Id == id).Include(s => s.Categories);
}
