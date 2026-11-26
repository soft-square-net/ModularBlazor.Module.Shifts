using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Roles;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Units;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;

/// <summary>
/// A published rota as grants in the host. Every change to a published shift (its rota, a check-out, its section's
/// categories, its move to a unit, or a change to one of its roles' permissions) rewrites the shift's grants in one go: <see cref="IGrantStore.ReplaceSourceAsync"/>
/// with source <c>shift:{id}</c>. Access then starts and ends on the clock: the host evaluates each grant's window per request.
/// </summary>
public sealed class ShiftGrants(IGrantStore grants, UnitCache units, ShiftRoles roles)
{
    /// <summary>
    /// Each member's role permissions (from the role definition that holds for the shift's unit, read now) × the
    /// shift's scopes (<see cref="ScopesAsync"/>), valid from the start to the member's end.
    /// </summary>
    public async Task WriteAsync(Shift shift, Section? section, CancellationToken ct)
    {
        if (!shift.Published) return;
        var scopes = await ScopesAsync(shift, section, ct);
        var set = await roles.ForUnitAsync(shift.UnitId, ct);
        var rows = shift.Members
            .Where(m => shift.EndOf(m) > shift.StartsAt)
            .SelectMany(m => set.PermissionsOf(m.RoleKey).SelectMany(permission => scopes.Select(scope =>
                new NewGrant(m.UserId, permission, scope, shift.StartsAt, shift.EndOf(m)))))
            .Distinct()
            .ToArray();
        await grants.ReplaceSourceAsync(shift.GrantSource, rows, ct);
    }

    /// <summary>
    /// The shift's unit scopes (<see cref="ShiftScopes.For"/>) when its unit is cached, plus its section's; only the
    /// section's for a shift that has no unit yet (or whose unit Organization removed).
    /// </summary>
    public async Task<IReadOnlyList<string>> ScopesAsync(Shift shift, Section? section, CancellationToken ct)
    {
        if (shift.UnitId is { } unitId && await units.FindAsync(unitId, ct) is { } unit)
            return ShiftScopes.For(unit, await units.CategoriesOfAsync(unit, ct), section);
        return section?.Scopes ?? [];
    }

    /// <summary>Removes every grant of the shift.</summary>
    public Task ClearAsync(Shift shift, CancellationToken ct) => grants.ReplaceSourceAsync(shift.GrantSource, [], ct);
}
