namespace soft.square.net.WebApi.Modules.Shifts.UseCases;

/// <summary>The shifts module's own permissions (standing grants, from admins).</summary>
public static class ShiftPermissions
{
    public const string View = "shifts.view";
    public const string Manage = "shifts.manage";

    public static IReadOnlyList<PermissionDefinition> All { get; } =
    [
        new(View, "See sections, shifts and rotas."),
        new(Manage, "Manage sections, plan and publish shifts, check shifts out.")
    ];
}
