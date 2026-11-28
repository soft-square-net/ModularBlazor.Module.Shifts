using soft.square.net.WebApi.Modules.Shifts.Client;

namespace soft.square.net.Module.Shifts.Components;

/// <summary>What the role editor edits.</summary>
public sealed class RoleForm
{
    /// <summary>Null for a new role.</summary>
    public int? Id { get; init; }

    public string Key { get; set; } = "";
    public string NameEn { get; set; } = "";
    public string NameAr { get; set; } = "";
    public List<string> Permissions { get; set; } = [];
    public bool IsLead { get; set; }

    /// <summary>The lead flag as saved.</summary>
    public bool WasLead { get; init; }

    /// <summary>The unit, as the picker holds it ("" for the whole tenant).</summary>
    public string UnitId { get; set; } = "";

    public static RoleForm New() => new();

    public static RoleForm From(RoleDefinitionDto role) => new()
    {
        Id = role.Id,
        Key = role.Key,
        NameEn = role.NameEn,
        NameAr = role.NameAr,
        Permissions = [.. role.Permissions],
        IsLead = role.IsLead,
        WasLead = role.IsLead,
        UnitId = role.UnitId?.ToString() ?? "",
    };

    /// <summary>A tenant-wide role that takes over from a unit's: the same key and lead flag.</summary>
    public static RoleForm HeirOf(RoleDefinitionDto role)
    {
        var form = From(role).AsNew();
        form.Key = role.Key;
        form.UnitId = "";
        return form;
    }

    /// <summary>A new role with these contents; its key is left to type.</summary>
    public RoleForm AsNew() => new()
    {
        NameEn = NameEn,
        NameAr = NameAr,
        Permissions = [.. Permissions],
        IsLead = IsLead,
        UnitId = UnitId,
    };

    public RoleRequest ToRequest() => new()
    {
        Key = Key.Trim().ToLowerInvariant(),
        NameEn = NameEn.Trim(),
        NameAr = NameAr.Trim(),
        Permissions = Permissions,
        IsLead = IsLead,
        UnitId = Guid.TryParse(UnitId, out var unit) ? unit : null,
    };

    public RoleUpdateRequest ToUpdate() => new()
    {
        NameEn = NameEn.Trim(),
        NameAr = NameAr.Trim(),
        Permissions = Permissions,
        IsLead = IsLead,
    };
}

/// <summary>Which of the API's refusals a role change ran into.</summary>
public static class RoleConflicts
{
    /// <summary>An update's only 409 is a refused change of the lead flag.</summary>
    public static bool IsLeadRefusal(int status, int? id, bool isLead, bool wasLead) => status == 409 && id is not null && isLead != wasLead;
}
