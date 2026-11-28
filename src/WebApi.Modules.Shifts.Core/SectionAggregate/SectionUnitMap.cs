namespace soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;

/// <summary>
/// A section moved to Organization: the Section unit that replaces it (table <c>section_unit_map</c>, one row per
/// section). Written by "Move sections to Organization"; a moved section's shifts are granted on its unit instead of on <c>section:{int}</c>.
/// </summary>
public sealed class SectionUnitMap : AggregateRoot<int>, ITenantOwned
{
    private SectionUnitMap() { } // EF Core

    public SectionUnitMap(int sectionId, Guid unitId, DateTimeOffset movedAt, string movedBy)
    {
        if (unitId == Guid.Empty) throw new DomainException("A moved section needs a unit.");
        SectionId = sectionId;
        UnitId = unitId;
        MovedAt = movedAt.ToUniversalTime();
        MovedBy = movedBy;
    }

    public string TenantId { get; set; } = "";
    public int SectionId { get; private set; }
    public Guid UnitId { get; private set; }
    public DateTimeOffset MovedAt { get; private set; }
    public string MovedBy { get; private set; } = "";
}
