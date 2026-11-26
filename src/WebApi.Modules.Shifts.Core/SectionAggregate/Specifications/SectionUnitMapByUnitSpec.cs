namespace soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate.Specifications;

/// <summary>The section that moved to a unit, if any.</summary>
public sealed class SectionUnitMapByUnitSpec : Specification<SectionUnitMap>
{
    public SectionUnitMapByUnitSpec(Guid unitId) => Query.Where(m => m.UnitId == unitId).OrderBy(m => m.SectionId);
}
