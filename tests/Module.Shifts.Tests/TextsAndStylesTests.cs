using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using soft.square.net.Module.Shifts.Pages;
using soft.square.net.ModularBlazor.Abstractions;

namespace soft.square.net.Module.Shifts.Tests;

/// <summary>
/// Arabic and English: every text the module shows has an Arabic entry (the keys are the English texts, so English
/// needs no file), and the file ships inside the module. RTL and the theme: the stylesheet uses logical sides only and
/// theme tokens for colours.
/// </summary>
public partial class TextsAndStylesTests
{
    private static readonly string ModuleDir = Path.Combine(RepoRoot(), "src", "Module.Shifts");

    private static string RepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "ModularBlazor.Module.Shifts.slnx"))) return dir.FullName;
        throw new InvalidOperationException("The repository root (ModularBlazor.Module.Shifts.slnx) was not found.");
    }

    private static IEnumerable<string> Sources(string pattern) =>
        Directory.EnumerateFiles(ModuleDir, pattern, SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

    private static Dictionary<string, string> Arabic() =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(ModuleDir, "i18n", "ar.json")))!;

    /// <summary>L["..."] and the forms' problems in the sources, plus the texts that are looked up by variable.</summary>
    private static IReadOnlyList<string> Keys()
    {
        var keys = new List<string>();
        foreach (var file in Sources("*.razor").Concat(Sources("*.cs")))
        {
            var text = File.ReadAllText(file);
            foreach (var pattern in new[] { LocalizedText(), FormProblem(), FieldProblem() })
                keys.AddRange(pattern.Matches(text).Select(m => Regex.Unescape(m.Groups[1].Value)));
        }

        keys.AddRange(ShiftState.Statuses);
        keys.AddRange(ShiftState.AttendanceStatuses.Select(ShiftState.AttendanceText));
        keys.AddRange(ShiftsNav.Pages.Select(p => p.Title));
        keys.AddRange(RoleChoices.Seeded.Select(s => s.Text));

        var fields = (System.Collections.IEnumerable)typeof(UnitAttendance)
            .GetField("Fields", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        foreach (var field in fields)
            foreach (var name in new[] { "Title", "Help" })
                keys.Add((string)field.GetType().GetProperty(name)!.GetValue(field)!);

        var statusText = typeof(MoveSections).GetMethod("StatusText", BindingFlags.NonPublic | BindingFlags.Static)!;
        keys.AddRange(Enum.GetValues<SectionMoveStatus>().Select(s => (string)statusText.Invoke(null, [s])!));

        var assembly = typeof(ShiftsPageBase).Assembly;
        keys.AddRange(assembly.GetCustomAttributes<MenuLinkAttribute>().Select(a => a.Title));
        keys.AddRange(assembly.GetCustomAttributes<MenuGroupAttribute>().Select(a => a.Title));
        keys.Add(assembly.GetCustomAttribute<ClientModuleAttribute>()!.NavTitle!);
        return [.. keys.Where(k => !string.IsNullOrEmpty(k)).Distinct()];
    }

    [GeneratedRegex(@"\bL\[""((?:[^""\\]|\\.)*)""")]
    private static partial Regex LocalizedText();

    [GeneratedRegex(@"ShiftFormException\(""((?:[^""\\]|\\.)*)""")]
    private static partial Regex FormProblem();

    [GeneratedRegex(@"Parse\(\w+, ""((?:[^""\\]|\\.)*)""\)")]
    private static partial Regex FieldProblem();

    [Fact]
    public void Every_text_has_an_arabic_translation()
    {
        var arabic = Arabic();
        var keys = Keys();

        keys.Count.ShouldBeGreaterThan(200);
        keys.Where(k => !arabic.ContainsKey(k)).ShouldBeEmpty();
    }

    [Fact]
    public void No_translation_is_left_over()
    {
        var keys = Keys().ToHashSet(StringComparer.Ordinal);

        Arabic().Keys.Where(k => k != "$comment" && !keys.Contains(k)).ShouldBeEmpty();
    }

    [Fact]
    public void Translations_keep_their_placeholders()
    {
        foreach (var (key, text) in Arabic().Where(e => e.Key != "$comment"))
        {
            var expected = Placeholder().Matches(key).Select(m => m.Value).Order().ToList();
            Placeholder().Matches(text).Select(m => m.Value).Order().ToList().ShouldBe(expected, $"\"{key}\"");
            text.ShouldNotBeNullOrWhiteSpace(key);
        }
    }

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex Placeholder();

    [Fact]
    public void The_arabic_file_ships_inside_the_module()
    {
        using var stream = typeof(ShiftsPageBase).Assembly.GetManifestResourceStream("i18n/ar.json");

        stream.ShouldNotBeNull();
        using var reader = new StreamReader(stream);
        JsonSerializer.Deserialize<Dictionary<string, string>>(reader.ReadToEnd())!.ShouldContainKey("Planner");
    }

    [Fact]
    public void The_stylesheet_uses_logical_sides_so_it_mirrors_in_rtl()
    {
        var css = File.ReadAllText(Path.Combine(ModuleDir, "wwwroot", "shifts.css"));

        PhysicalSide().Matches(css).Select(m => m.Value).ShouldBeEmpty();
    }

    [GeneratedRegex(@"(?:(?:margin|padding|border)-(?:left|right)|(?<![-\w])(?:left|right)\s*:|text-align\s*:\s*(?:left|right)|float\s*:\s*(?:left|right))")]
    private static partial Regex PhysicalSide();

    [Fact]
    public void Colours_come_from_the_theme_tokens()
    {
        var css = File.ReadAllText(Path.Combine(ModuleDir, "wwwroot", "shifts.css"));

        HexColour().Matches(css).Select(m => m.Value).ShouldBeEmpty();
        RawColourFunction().Matches(css).Select(m => m.Value).ShouldBeEmpty();
        Regex.Matches(css, @"var\(--(?!mud-)[\w-]+").Select(m => m.Value).ShouldBeEmpty();
    }

    [GeneratedRegex(@"#[0-9a-fA-F]{3,8}\b")]
    private static partial Regex HexColour();

    [GeneratedRegex(@"\b(?:rgba?|hsla?)\((?!\s*var\()")]
    private static partial Regex RawColourFunction();

    [Fact]
    public void Every_page_is_on_the_module_route_prefix()
    {
        var routes = typeof(ShiftsPageBase).Assembly.GetTypes()
            .SelectMany(t => t.GetCustomAttributes<Microsoft.AspNetCore.Components.RouteAttribute>())
            .Select(r => r.Template).Order(StringComparer.Ordinal).ToList();

        routes.ShouldBe(["/shifts", "/shifts/attendance", "/shifts/mine", "/shifts/roles", "/shifts/sections",
            "/shifts/sections/move", "/shifts/templates", "/shifts/{Id:int}"]);
    }
}
