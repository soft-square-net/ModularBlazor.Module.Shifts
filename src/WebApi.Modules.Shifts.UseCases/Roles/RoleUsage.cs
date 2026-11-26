using soft.square.net.Organization.Contracts;
using soft.square.net.WebApi.Modules.Shifts.Core.RoleAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.RoleAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.Core.TemplateAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.TemplateAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Units;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Roles;

/// <summary>
/// Where a role definition is in force: the shifts that have not ended and the templates whose unit resolves the
/// definition's key to this very definition (an override nearer the unit wins over it). Pass the definition as one of
/// the <c>definitions</c> list (the same instance): resolution compares references.
/// </summary>
public sealed class RoleUsage(IReadRepository<ShiftRoleDefinition> roles, IReadRepository<Shift> shifts, IReadRepository<ShiftTemplate> templates,
    UnitCache units, ShiftCaller caller)
{
    /// <summary>The shifts that have not ended and use <paramref name="definition"/> (resolved with <paramref name="definitions"/>).</summary>
    public async Task<IReadOnlyList<Shift>> ShiftsAsync(ShiftRoleDefinition definition, IReadOnlyList<ShiftRoleDefinition> definitions,
        CancellationToken ct)
    {
        var candidates = await shifts.ListAsync(new ShiftsNotEndedWithRoleSpec(definition.Key, caller.Now), ct);
        var used = new List<Shift>();
        foreach (var shift in candidates)
            if (await ResolvesToAsync(shift.UnitId, definition, definitions, ct)) used.Add(shift);
        return used;
    }

    /// <summary>The templates that use <paramref name="definition"/> in a slot or for a default member.</summary>
    public async Task<IReadOnlyList<ShiftTemplate>> TemplatesAsync(ShiftRoleDefinition definition, IReadOnlyList<ShiftRoleDefinition> definitions,
        CancellationToken ct)
    {
        var used = new List<ShiftTemplate>();
        foreach (var template in await templates.ListAsync(new TemplatesSpec(), ct))
            if ((template.Slots.Any(s => s.RoleKey == definition.Key) || template.DefaultMembers.Any(m => m.RoleKey == definition.Key))
                && await ResolvesToAsync(template.UnitId, definition, definitions, ct))
                used.Add(template);
        return used;
    }

    /// <summary>Every definition of the tenant (to resolve with).</summary>
    public async Task<IReadOnlyList<ShiftRoleDefinition>> AllAsync(CancellationToken ct) => await roles.ListAsync(new AllRoleDefinitionsSpec(), ct);

    /// <summary>The definition the key resolves to for a unit, among <paramref name="definitions"/>.</summary>
    public async Task<ShiftRoleDefinition?> ResolveAsync(Guid? unitId, string key, IEnumerable<ShiftRoleDefinition> definitions, CancellationToken ct) =>
        RoleSet.Resolve(definitions, await PathIdsAsync(unitId, ct)).Find(key);

    private async Task<bool> ResolvesToAsync(Guid? unitId, ShiftRoleDefinition definition, IReadOnlyList<ShiftRoleDefinition> definitions,
        CancellationToken ct) =>
        ReferenceEquals(await ResolveAsync(unitId, definition.Key, definitions, ct), definition);

    private async Task<IReadOnlyList<string>> PathIdsAsync(Guid? unitId, CancellationToken ct) =>
        unitId is { } id && await units.FindAsync(id, ct) is { } unit ? UnitPaths.Ids(unit.Path) : [];
}
