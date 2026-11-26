using System.Globalization;
using soft.square.net.WebApi.Modules.Shifts.Client;

namespace soft.square.net.Module.Shifts.Components;

/// <summary>What the shift editor edits: a section, a local start and end time and the rota.</summary>
/// <remarks>
/// The times are the datetime-local inputs' own text ("yyyy-MM-ddTHH:mm"), parsed with the invariant culture: binding
/// a DateTime parses with the current culture, which rejects the browser's value under some cultures (calendar, time
/// separator), and the input silently snaps back.
/// </remarks>
public sealed class ShiftForm
{
    public const string InputFormat = "yyyy-MM-ddTHH:mm";

    public string SectionId { get; set; } = "";
    public string StartsAt { get; set; } = "";
    public string EndsAt { get; set; } = "";
    public List<RotaRow> Rota { get; set; } = [];

    public sealed class RotaRow
    {
        public string UserId { get; set; } = "";
        /// <summary>The key of a role definition that holds for the shift's unit (GET /roles/effective).</summary>
        public string RoleKey { get; set; } = "desk";
    }

    /// <summary>A new shift: the next full hour for eight hours, in the only section if there is one.</summary>
    public static ShiftForm New(IReadOnlyList<SectionDto> sections, string? me)
    {
        var start = DateTime.Now.Date.AddHours(DateTime.Now.Hour + 1);
        return new ShiftForm
        {
            SectionId = sections.Count == 1 ? sections[0].Id.ToString(CultureInfo.InvariantCulture) : "",
            StartsAt = Format(start),
            EndsAt = Format(start.AddHours(8)),
            Rota = me is null ? [] : [new RotaRow { UserId = me, RoleKey = "manager" }],
        };
    }

    public static ShiftForm From(ShiftDto shift) => new()
    {
        SectionId = shift.SectionId?.ToString(CultureInfo.InvariantCulture) ?? "",
        StartsAt = Format(shift.StartsAt.ToLocalTime().DateTime),
        EndsAt = Format(shift.EndsAt.ToLocalTime().DateTime),
        Rota = shift.Rota.Select(m => new RotaRow { UserId = m.UserId, RoleKey = m.RoleKey }).ToList(),
    };

    /// <summary>The request, or why the form cannot become one.</summary>
    public ShiftRequest ToRequest()
    {
        if (!int.TryParse(SectionId, NumberStyles.None, CultureInfo.InvariantCulture, out var sectionId))
            throw new FormatException("Choose a section.");
        return new ShiftRequest
        {
            SectionId = sectionId,
            StartsAt = Parse(StartsAt, "start"),
            EndsAt = Parse(EndsAt, "end"),
            Rota = Rota.Where(r => !string.IsNullOrWhiteSpace(r.UserId))
                .Select(r => new RotaEntry { UserId = r.UserId.Trim(), RoleKey = r.RoleKey }).ToList(),
        };
    }

    private static string Format(DateTime local) => local.ToString(InputFormat, CultureInfo.InvariantCulture);

    private static DateTimeOffset Parse(string text, string what)
    {
        // Browsers send minutes, or seconds when the step allows them.
        if (!DateTime.TryParseExact(text, [InputFormat, InputFormat + ":ss"], CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal, out var local))
            throw new FormatException($"Enter the shift's {what} as a date and time.");
        return new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Local));
    }
}
