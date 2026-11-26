using soft.square.net.Organization.Contracts;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Units;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Authority;

/// <summary>
/// Who may manage what. <see cref="ShiftPermissions.Manage"/> is checked on the unit's scopes and its ancestors'
/// (<see cref="UnitScopes.For"/>), so a grant on <c>unit:{department}</c> covers the Department's Sections. Things with
/// no unit (shifts planned before the move, tenant-wide role definitions, sections, the unit cache) need it
/// tenant-wide. The endpoints only require the permission in some scope; the use cases check the scope here.
/// </summary>
public sealed class ShiftAuthority(IAccessScopes access, UnitCache units)
{
    /// <summary>The scopes <see cref="ShiftPermissions.Manage"/> is checked on for a unit; none (tenant-wide only) without a cached unit.</summary>
    public async Task<IReadOnlyList<string>> ScopesAsync(Guid? unitId, CancellationToken ct) =>
        unitId is { } id && await units.FindAsync(id, ct) is { } unit ? UnitScopes.For(unit.Path) : [];

    public async Task<bool> CanManageAsync(Guid? unitId, CancellationToken ct) =>
        await access.HasPermissionAsync(ShiftPermissions.Manage, await ScopesAsync(unitId, ct), ct);

    /// <summary>Success, or Forbidden naming what the caller lacks.</summary>
    public async Task<Result> RequireManageAsync(Guid? unitId, CancellationToken ct) =>
        await CanManageAsync(unitId, ct)
            ? Result.Success()
            : Result.Forbidden(unitId is { } id
                ? $"Managing unit {id} needs {ShiftPermissions.Manage} on it or on one of its parent units."
                : $"This needs {ShiftPermissions.Manage} for the whole tenant.");

    /// <summary>Success when the caller holds <see cref="ShiftPermissions.Manage"/> tenant-wide.</summary>
    public Task<Result> RequireTenantManageAsync(CancellationToken ct) => RequireManageAsync(null, ct);
}
