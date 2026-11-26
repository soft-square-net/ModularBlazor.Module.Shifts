using soft.square.net.Organization.Contracts;
using soft.square.net.WebApi.Modules.Shifts.Core.RoleAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.RoleAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Units;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Roles;

/// <summary>
/// The role definitions that hold for a unit: for each key, the nearest definition up the unit's path (the unit's own,
/// else its parent's, and so on, else the tenant's). A shift with no unit (or whose unit is not cached) sees the
/// tenant's definitions only.
/// </summary>
public sealed class ShiftRoles(IReadRepository<ShiftRoleDefinition> roles, UnitCache units)
{
    public async Task<RoleSet> ForUnitAsync(Guid? unitId, CancellationToken ct)
    {
        var path = unitId is { } id && await units.FindAsync(id, ct) is { } unit ? UnitPaths.Ids(unit.Path) : [];
        return RoleSet.Resolve(await roles.ListAsync(new AllRoleDefinitionsSpec(), ct), path);
    }
}

/// <summary>The effective role definitions of one unit, by key.</summary>
public sealed class RoleSet(IReadOnlyDictionary<string, ShiftRoleDefinition> byKey)
{
    public IReadOnlyDictionary<string, ShiftRoleDefinition> ByKey { get; } = byKey;

    public ShiftRoleDefinition? Find(string key) => ByKey.GetValueOrDefault(key);

    /// <summary>The permissions of a role key here; none for a key no definition holds for any more.</summary>
    public IReadOnlyList<string> PermissionsOf(string key) => Find(key)?.Permissions ?? [];

    /// <summary>For each key, the definition nearest to the unit at <paramref name="unitPathIds"/> (ids, root first).</summary>
    public static RoleSet Resolve(IEnumerable<ShiftRoleDefinition> definitions, IReadOnlyList<string> unitPathIds)
    {
        int Depth(ShiftRoleDefinition d) => d.UnitId is { } unit ? IndexOf(unitPathIds, UnitPaths.Id(unit)) : -1;
        var byKey = definitions.Where(d => d.HoldsFor(unitPathIds))
            .GroupBy(d => d.Key, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.MaxBy(Depth)!, StringComparer.Ordinal);
        return new RoleSet(byKey);
    }

    private static int IndexOf(IReadOnlyList<string> ids, string id)
    {
        for (var i = 0; i < ids.Count; i++)
            if (string.Equals(ids[i], id, StringComparison.Ordinal)) return i;
        return -1;
    }
}
