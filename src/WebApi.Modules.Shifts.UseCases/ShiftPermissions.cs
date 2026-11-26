namespace soft.square.net.WebApi.Modules.Shifts.UseCases;

/// <summary>The shifts module's own permissions (standing grants, from admins).</summary>
public static class ShiftPermissions
{
    public const string View = "shifts.view";
    public const string Manage = "shifts.manage";

    public static IReadOnlyList<PermissionDefinition> All { get; } =
    [
        new(View, "See sections, shifts, rotas, role definitions and templates."),
        new(Manage, "Plan, publish and check out shifts, and manage role definitions and templates, of the units it is granted on (and the units below them); tenant-wide also sections, tenant-wide roles and the unit cache.")
    ];
}
