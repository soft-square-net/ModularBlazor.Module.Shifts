using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases;

/// <summary>
/// What each rota role grants for the section's categories while the shift runs. The permission names belong to the
/// content module (see its ContentPermissions); they are plain strings, so neither module references the other.
/// </summary>
public static class RoleTemplates
{
    public static IReadOnlyDictionary<ShiftRole, string[]> Permissions { get; } = new Dictionary<ShiftRole, string[]>
    {
        [ShiftRole.Manager] = ["content.accept", "content.assign", "content.transfer", "content.return"],
        [ShiftRole.Desk] = ["content.publish", "content.return"],
        [ShiftRole.Editor] = ["content.review"]
    };
}
