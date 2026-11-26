using System.Text.RegularExpressions;

namespace soft.square.net.WebApi.Modules.Shifts.Core.RoleAggregate;

/// <summary>
/// A rota role as data (table <c>shift_roles</c>): what a member in this role is granted while the shift runs
/// (<see cref="Permissions"/>, names from the platform's permission catalog), and whether it is the shift's lead.
/// A definition with no <see cref="UnitId"/> holds for the whole tenant; one on a unit overrides the tenant's (or an
/// ancestor's) definition of the same key for that unit and every unit below it: the nearest one up the unit's path wins.
/// </summary>
public sealed partial class ShiftRoleDefinition : AggregateRoot<int>, ITenantOwned
{
    public const int KeyLength = 64;
    public const int NameLength = 200;
    public const int PermissionLength = 128;
    public const int MaxPermissions = 50;

    private ShiftRoleDefinition() { } // EF Core

    public string TenantId { get; set; } = "";
    /// <summary>The role's key on rotas and templates: a lowercase slug (<c>manager</c>, <c>desk</c>, <c>photo-editor</c>).</summary>
    public string Key { get; private set; } = "";
    public string NameEn { get; private set; } = "";
    public string NameAr { get; private set; } = "";
    /// <summary>The permissions a member in this role holds on the shift's scopes while their access runs.</summary>
    public IReadOnlyList<string> Permissions { get; private set; } = [];
    /// <summary>The shift's lead (a shift has exactly one lead when it is published).</summary>
    public bool IsLead { get; private set; }
    /// <summary>The Organization unit the definition holds for (and below); null for the whole tenant.</summary>
    public Guid? UnitId { get; private set; }

    public static ShiftRoleDefinition Create(string key, string nameEn, string nameAr, IEnumerable<string> permissions, bool isLead, Guid? unitId)
    {
        if (unitId == Guid.Empty) throw new DomainException("A role's unit cannot be empty; leave it out for the whole tenant.");
        var definition = new ShiftRoleDefinition { Key = NormalizeKey(key), UnitId = unitId };
        definition.Update(nameEn, nameAr, permissions, isLead);
        return definition;
    }

    public void Update(string nameEn, string nameAr, IEnumerable<string> permissions, bool isLead)
    {
        NameEn = Name(nameEn, "English");
        NameAr = string.IsNullOrWhiteSpace(nameAr) ? NameEn : Name(nameAr, "Arabic");
        var list = permissions.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        if (list.Length > MaxPermissions) throw new DomainException($"A role grants at most {MaxPermissions} permissions.");
        if (list.FirstOrDefault(p => p.Length > PermissionLength) is { } tooLong) throw new DomainException($"Permission '{tooLong}' is too long.");
        Permissions = list;
        IsLead = isLead;
    }

    /// <summary>True when the definition holds for a unit at <paramref name="unitPathIds"/> (its ids, root first), or for a shift with no unit.</summary>
    public bool HoldsFor(IReadOnlyList<string> unitPathIds) =>
        UnitId is not { } unit || unitPathIds.Contains(unit.ToString("N"), StringComparer.Ordinal);

    /// <summary>The key as stored: trimmed, lowercase; letters, digits and dashes, starting with a letter.</summary>
    public static string NormalizeKey(string key)
    {
        var normalized = (key ?? "").Trim().ToLowerInvariant();
        if (normalized.Length is 0 or > KeyLength || !KeyPattern().IsMatch(normalized))
            throw new DomainException($"Role key '{key}' is not valid: use lowercase letters, digits and dashes, starting with a letter (at most {KeyLength}).");
        return normalized;
    }

    public static bool IsValidKey(string? key) =>
        key is { Length: > 0 and <= KeyLength } && KeyPattern().IsMatch(key);

    private static string Name(string value, string language)
    {
        var name = (value ?? "").Trim();
        if (name.Length == 0) throw new DomainException($"A role needs an {language} name.");
        if (name.Length > NameLength) throw new DomainException($"A role's {language} name has at most {NameLength} characters.");
        return name;
    }

    [GeneratedRegex("^[a-z][a-z0-9-]*$")]
    private static partial Regex KeyPattern();
}
