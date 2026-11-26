using soft.square.net.WebApi.Modules.Shifts.Core.RoleAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Roles;

/// <summary>
/// The tenant-wide role definitions every tenant starts with: the three roles the rota had before roles became data,
/// with the same content permissions. The permission names belong to the content module (its ContentPermissions); they
/// are plain strings, so neither module references the other. They are seeded once per tenant (when it has no role
/// definitions at all, see <see cref="SeedShiftRoles"/>); after that they are ordinary rows an admin may change.
/// </summary>
public static class DefaultShiftRoles
{
    public const string Manager = "manager";
    public const string Desk = "desk";
    public const string Editor = "editor";

    public static IReadOnlyList<ShiftRoleDefinition> Create() =>
    [
        ShiftRoleDefinition.Create(Manager, "Manager", "مدير الوردية", ["content.accept", "content.assign", "content.transfer", "content.return"], isLead: true, null),
        ShiftRoleDefinition.Create(Desk, "Desk", "الديسك", ["content.publish", "content.return"], isLead: false, null),
        ShiftRoleDefinition.Create(Editor, "Editor", "محرر", ["content.review"], isLead: false, null)
    ];
}

/// <summary>Seeds <see cref="DefaultShiftRoles"/> into a tenant that has no role definitions yet (at tenant start).</summary>
public sealed class SeedShiftRoles(IRepository<ShiftRoleDefinition> roles)
{
    /// <summary>True when the defaults were added.</summary>
    public async Task<bool> SeedAsync(CancellationToken ct)
    {
        if (await roles.AnyAsync(ct)) return false;
        await roles.AddRangeAsync(DefaultShiftRoles.Create(), ct);
        return true;
    }
}
