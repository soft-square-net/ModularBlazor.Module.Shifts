using soft.square.net.WebApi.Modules.Shifts.Client;

namespace soft.square.net.Module.Shifts.Model;

/// <summary>One choice of a role select, or the name shown for a rota member's role.</summary>
public sealed record RoleChoice(string Key, string Name, bool IsLead);

/// <summary>The roles a rota offers: the definitions that hold for the shift's unit (GET /roles/effective).</summary>
public static class RoleChoices
{
    /// <summary>The roles every tenant is seeded with, offered when the definitions cannot be read (key, text, lead).</summary>
    public static readonly (string Key, string Text, bool IsLead)[] Seeded =
        [("manager", "Manager", true), ("desk", "Desk", false), ("editor", "Editor", false)];

    public static string NameOf(RoleDefinitionDto role, bool arabic) =>
        arabic && !string.IsNullOrWhiteSpace(role.NameAr) ? role.NameAr! : role.NameEn;

    /// <summary>
    /// The choices of one select. A role the member already has is always among them, so that saving a shift never
    /// changes a role silently because its definition could not be read.
    /// </summary>
    public static IReadOnlyList<RoleChoice> For(IReadOnlyList<RoleDefinitionDto> roles, string? current, bool arabic, Func<string, string> text)
    {
        List<RoleChoice> choices = roles.Count > 0
            ? [.. roles.Select(r => new RoleChoice(r.Key, NameOf(r, arabic), r.IsLead))]
            : [.. Seeded.Select(s => new RoleChoice(s.Key, text(s.Text), s.IsLead))];
        if (!string.IsNullOrWhiteSpace(current) && choices.All(c => c.Key != current))
            choices.Add(new RoleChoice(current, current, false));
        return choices;
    }

    /// <summary>The name to show for a role key.</summary>
    public static string Name(IReadOnlyList<RoleDefinitionDto> roles, string key, bool arabic, Func<string, string> text) =>
        For(roles, key, arabic, text).First(c => c.Key == key).Name;
}
