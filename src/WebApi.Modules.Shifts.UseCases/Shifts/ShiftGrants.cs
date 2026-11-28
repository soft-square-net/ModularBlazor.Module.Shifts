using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Attendance;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Roles;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Units;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;

/// <summary>
/// The checked-in members of a published shift as grants in the host. Every change to a published shift (a check-in or
/// check-out, its rota, its end, its section's categories, its move to a unit, or a change to one of its roles'
/// permissions) rewrites the shift's grants in one go: <see cref="IGrantStore.ReplaceSourceAsync"/> with source
/// <c>shift:{id}</c>. Each write clears the users' cached rights on every replica, and the live hub drops the topics
/// they may no longer receive (so a check-out revokes at once). Within a window, access starts and ends on the clock:
/// the host evaluates each grant's window per request.
/// </summary>
public sealed class ShiftGrants(IGrantStore grants, UnitCache units, ShiftRoles roles, ShiftThresholds thresholds,
    IReadRepository<SectionUnitMap> maps)
{
    /// <summary>
    /// Each checked-in member's role permissions (from the role definition that holds for the shift's unit, read now)
    /// × the shift's scopes (<see cref="ScopesAsync"/>), valid from their check-in to their check-out, or to the end plus
    /// the check-out grace while they are in (<see cref="ShiftMember.AccessOn"/>). Nobody else gets a grant.
    /// </summary>
    public async Task WriteAsync(Shift shift, Section? section, CancellationToken ct)
    {
        if (!shift.Published) return;
        var grace = (await thresholds.ForAsync(shift, section, ct)).CheckoutGrace;
        var windows = shift.Members.Select(m => (Member: m, Window: m.AccessOn(shift, grace))).Where(x => x.Window is not null).ToArray();
        NewGrant[] rows = [];
        if (windows.Length > 0)
        {
            var scopes = await ScopesAsync(shift, section, ct);
            var set = await roles.ForUnitAsync(shift.UnitId, ct);
            rows = windows
                .SelectMany(x => set.PermissionsOf(x.Member.RoleKey).SelectMany(permission => scopes.Select(scope =>
                    new NewGrant(x.Member.UserId, permission, scope, x.Window!.Value.From, x.Window.Value.To))))
                .Distinct()
                .ToArray();
        }
        await grants.ReplaceSourceAsync(shift.GrantSource, rows, ct);
    }

    /// <summary>
    /// The shift's unit scopes (<see cref="ShiftScopes.For"/>) when its unit is cached, plus its section's (without the
    /// legacy <c>section:{sectionId}</c> once the section has moved); only the section's, all of them, for a shift that
    /// has no unit yet (or whose unit Organization removed), so such a shift never ends up with no scope.
    /// </summary>
    public async Task<IReadOnlyList<string>> ScopesAsync(Shift shift, Section? section, CancellationToken ct)
    {
        if (shift.UnitId is { } unitId && await units.FindAsync(unitId, ct) is { } unit)
        {
            var moved = section is not null && await maps.FirstOrDefaultAsync(new SectionUnitMapsSpec(section.Id), ct) is not null;
            return ShiftScopes.For(unit, await units.CategoriesOfAsync(unit, ct), section, moved);
        }
        return section?.Scopes ?? [];
    }

    /// <summary>Removes every grant of the shift.</summary>
    public Task ClearAsync(Shift shift, CancellationToken ct) => grants.ReplaceSourceAsync(shift.GrantSource, [], ct);
}
