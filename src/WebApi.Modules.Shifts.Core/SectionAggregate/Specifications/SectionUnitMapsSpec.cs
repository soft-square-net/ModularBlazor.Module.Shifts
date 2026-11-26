namespace soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate.Specifications;

/// <summary>The section-to-unit rows of the tenant (one section's, when given).</summary>
public sealed class SectionUnitMapsSpec : Specification<SectionUnitMap>
{
    public SectionUnitMapsSpec(int? sectionId = null)
    {
        if (sectionId is { } id) Query.Where(m => m.SectionId == id);
        Query.OrderBy(m => m.SectionId);
    }
}
