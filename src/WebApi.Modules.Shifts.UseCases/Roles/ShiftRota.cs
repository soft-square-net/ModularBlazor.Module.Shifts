using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Units;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Roles;

/// <summary>
/// A planned rota checked and resolved for a unit:
/// <list type="bullet">
/// <item>every role key must name a role definition that holds for the unit (<see cref="ShiftRoles"/>); keys are
/// compared in lowercase, so the old role names (<c>Manager</c>, <c>Desk</c>, <c>Editor</c>) still work;</item>
/// <item>for a shift of a unit, every person must be a member of the unit or of one of its ancestors, with a sign-in
/// identity (<see cref="UnitMembers"/>, decision D6). A shift with no unit (planned before its section moved) skips this.</item>
/// </list>
/// </summary>
public sealed class ShiftRota(ShiftRoles roles, UnitCache units, UnitMembers members)
{
    public async Task<Result<IReadOnlyList<RotaAssignment>>> ResolveAsync(Guid? unitId, IEnumerable<RotaEntry> rota, DateTimeOffset at,
        CancellationToken ct)
    {
        var entries = rota.Where(r => !string.IsNullOrWhiteSpace(r.UserId))
            .Select(r => (UserId: r.UserId.Trim(), Key: (r.RoleKey ?? "").Trim().ToLowerInvariant())).Distinct().ToArray();
        var set = await roles.ForUnitAsync(unitId, ct);

        var unknown = entries.Select(e => e.Key).Distinct().Where(k => set.Find(k) is null).ToArray();
        if (unknown.Length > 0)
            return Result.Invalid(new ValidationError("rota",
                $"No role definition holds here for: {string.Join(", ", unknown.Select(k => k.Length == 0 ? "(empty)" : k))}. Roles: {string.Join(", ", set.ByKey.Keys.Order())}."));

        if (unitId is { } id && entries.Length > 0 && await units.FindAsync(id, ct) is { } unit)
        {
            var eligible = await members.EligibleAsync(unit, at, ct);
            if (!eligible.IsSuccess) return eligible.As<IReadOnlySet<string>, IReadOnlyList<RotaAssignment>>();
            var outsiders = entries.Select(e => e.UserId).Distinct().Where(u => !eligible.Value.Contains(u)).ToArray();
            if (outsiders.Length > 0)
                return Result.Invalid(new ValidationError("rota",
                    $"Not a member of unit {unit.NameEn} or of a unit above it (with a sign-in identity): {string.Join(", ", outsiders)}."));
        }

        return entries.Select(e => new RotaAssignment(e.UserId, e.Key, set.Find(e.Key)!.IsLead)).ToArray();
    }

    /// <summary>Role keys (a template's slots) normalized, each checked to hold for the unit; in the order given.</summary>
    public async Task<Result<IReadOnlyList<string>>> ResolveKeysAsync(Guid? unitId, IEnumerable<string> keys, CancellationToken ct)
    {
        var normalized = keys.Select(k => (k ?? "").Trim().ToLowerInvariant()).ToArray();
        var set = await roles.ForUnitAsync(unitId, ct);
        var unknown = normalized.Distinct().Where(k => set.Find(k) is null).ToArray();
        if (unknown.Length > 0)
            return Result.Invalid(new ValidationError("slots",
                $"No role definition holds here for: {string.Join(", ", unknown.Select(k => k.Length == 0 ? "(empty)" : k))}."));
        return normalized;
    }
}
