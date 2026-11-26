namespace soft.square.net.WebApi.Modules.Shifts.Core.RoleAggregate.Specifications;

/// <summary>Every role definition of the tenant (tenant-wide ones first), by key.</summary>
public sealed class AllRoleDefinitionsSpec : Specification<ShiftRoleDefinition>
{
    public AllRoleDefinitionsSpec() => Query.OrderBy(r => r.UnitId != null).ThenBy(r => r.Key);
}

/// <summary>The role definition with this id.</summary>
public sealed class RoleDefinitionByIdSpec : SingleResultSpecification<ShiftRoleDefinition>
{
    public RoleDefinitionByIdSpec(int id) => Query.Where(r => r.Id == id);
}
