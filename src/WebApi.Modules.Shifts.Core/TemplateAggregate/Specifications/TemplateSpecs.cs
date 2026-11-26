namespace soft.square.net.WebApi.Modules.Shifts.Core.TemplateAggregate.Specifications;

/// <summary>The template with this id, its slots and default members.</summary>
public sealed class TemplateByIdSpec : SingleResultSpecification<ShiftTemplate>
{
    public TemplateByIdSpec(int id) => Query.Where(t => t.Id == id).Include(t => t.Slots).Include(t => t.DefaultMembers);
}

/// <summary>The templates, optionally of one unit, with their slots and default members, by name.</summary>
public sealed class TemplatesSpec : Specification<ShiftTemplate>
{
    public TemplatesSpec(Guid? unitId = null)
    {
        if (unitId is { } unit) Query.Where(t => t.UnitId == unit);
        Query.Include(t => t.Slots).Include(t => t.DefaultMembers).OrderBy(t => t.Name);
    }
}
