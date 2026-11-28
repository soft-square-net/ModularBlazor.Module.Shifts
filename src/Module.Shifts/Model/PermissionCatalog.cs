using System.Text.Json;

namespace soft.square.net.Module.Shifts.Model;

/// <summary>One permission of the platform's catalog.</summary>
public sealed record CatalogPermission(string Name, string? Description);

/// <summary>A module's permissions in the catalog.</summary>
public sealed record CatalogGroup(string ModuleId, string ModuleName, IReadOnlyList<CatalogPermission> Permissions);

/// <summary>
/// The platform's permission catalog (<c>GET api/identity/permissions</c>), which a role's permissions are picked from,
/// as the Organization module's position editor does. Reading it needs <c>access.grants.manage</c>; without it the
/// picker takes typed names, and the API checks them.
/// </summary>
public static class PermissionCatalog
{
    public const string Url = "api/identity/permissions";

    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    /// <summary>The catalog from the endpoint's JSON: groups and permissions sorted, empty groups left out.</summary>
    public static IReadOnlyList<CatalogGroup> Parse(string json) =>
    [
        .. (JsonSerializer.Deserialize<List<Wire>>(json, Web) ?? [])
            .Select(s => new CatalogGroup(s.ModuleId ?? "", string.IsNullOrWhiteSpace(s.ModuleName) ? s.ModuleId ?? "" : s.ModuleName,
                [.. (s.Permissions ?? []).Where(p => !string.IsNullOrWhiteSpace(p.Name)).DistinctBy(p => p.Name)
                    .OrderBy(p => p.Name, StringComparer.Ordinal).Select(p => new CatalogPermission(p.Name!, p.Description))]))
            .Where(g => g.Permissions.Count > 0)
            .OrderBy(g => g.ModuleName, StringComparer.OrdinalIgnoreCase)
    ];

    /// <summary>Permission names as people type them: lower case, dotted, no spaces.</summary>
    public static bool IsValidName(string name) =>
        name.Length is > 0 and <= 200 && name.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c is '.' or '-' or '_' or ':')
        && name[0] != '.' && name[^1] != '.';

    private sealed record Wire(string? ModuleId, string? ModuleName, List<WirePermission>? Permissions);

    private sealed record WirePermission(string? Name, string? Description);
}
