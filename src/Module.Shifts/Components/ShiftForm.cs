using System.Globalization;
using soft.square.net.Module.Shifts.Model;
using soft.square.net.WebApi.Modules.Shifts.Client;

namespace soft.square.net.Module.Shifts.Components;

/// <summary>What the shift editor edits: a section (or a unit), a start and end on the unit's clock and the rota.</summary>
/// <remarks>
/// The times are the datetime-local inputs' own text ("yyyy-MM-ddTHH:mm"), parsed with the invariant culture: binding
/// a DateTime parses with the current culture, which rejects the browser's value under some cultures (calendar, time
/// separator), and the input silently snaps back.
/// </remarks>
public sealed class ShiftForm
{
    public const string InputFormat = "yyyy-MM-ddTHH:mm";

    private DateTimeOffset? _startsAt;
    private DateTimeOffset? _endsAt;
    private string _startsText = "";
    private string _endsText = "";

    /// <summary>The zone the times are typed in: the unit's, else the browser's.</summary>
    public TimeZoneInfo Zone { get; init; } = TimeZoneInfo.Local;

    /// <summary>The unit a shift is planned for; null for one planned on a section that has not moved.</summary>
    public Guid? UnitId { get; init; }

    /// <summary>The section, as the select holds it ("" for none).</summary>
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

    /// <summary>A new shift of eight hours: from 09:00 on <paramref name="day"/> when given, else from the next full hour.</summary>
    public static ShiftForm New(TimeZoneInfo zone, string? me, Guid? unitId, IReadOnlyList<SectionDto>? sections = null,
        DateOnly? day = null, DateTimeOffset? now = null)
    {
        var local = ZoneTime.Local(now ?? DateTimeOffset.UtcNow, zone);
        var start = day?.ToDateTime(new TimeOnly(9, 0)) ?? local.Date.AddHours(local.Hour + 1);
        return new ShiftForm
        {
            Zone = zone,
            UnitId = unitId,
            SectionId = sections is { Count: 1 } ? sections[0].Id.ToString(CultureInfo.InvariantCulture) : "",
            StartsAt = Format(start),
            EndsAt = Format(start.AddHours(8)),
            Rota = me is null ? [] : [new RotaRow { UserId = me, RoleKey = "manager" }],
        };
    }

    public static ShiftForm From(ShiftDto shift, TimeZoneInfo zone)
    {
        var form = new ShiftForm
        {
            Zone = zone,
            UnitId = shift.UnitId,
            SectionId = shift.SectionId?.ToString(CultureInfo.InvariantCulture) ?? "",
            StartsAt = Format(ZoneTime.Local(shift.StartsAt, zone)),
            EndsAt = Format(ZoneTime.Local(shift.EndsAt, zone)),
            Rota = shift.Rota.Select(m => new RotaRow { UserId = m.UserId, RoleKey = m.RoleKey }).ToList(),
        };
        // An unchanged time is sent back as the very instant it was: a time of the repeated hour would otherwise move.
        (form._startsAt, form._startsText) = (shift.StartsAt, form.StartsAt);
        (form._endsAt, form._endsText) = (shift.EndsAt, form.EndsAt);
        return form;
    }

    /// <summary>The request, or why the form cannot become one (a text of the module, to translate).</summary>
    public ShiftRequest ToRequest()
    {
        int? sectionId = int.TryParse(SectionId, NumberStyles.None, CultureInfo.InvariantCulture, out var id) ? id : null;
        if (sectionId is null && UnitId is null) throw new ShiftFormException("Choose a section.");
        return new ShiftRequest
        {
            SectionId = sectionId,
            UnitId = UnitId,
            StartsAt = _startsAt is { } start && StartsAt == _startsText ? start : Parse(StartsAt, "Enter the shift's start as a date and time."),
            EndsAt = _endsAt is { } end && EndsAt == _endsText ? end : Parse(EndsAt, "Enter the shift's end as a date and time."),
            Rota = Rota.Where(r => !string.IsNullOrWhiteSpace(r.UserId))
                .Select(r => new RotaEntry { UserId = r.UserId.Trim(), RoleKey = r.RoleKey }).ToList(),
        };
    }

    private static string Format(DateTime local) => local.ToString(InputFormat, CultureInfo.InvariantCulture);

    private DateTimeOffset Parse(string text, string problem)
    {
        // Browsers send minutes, or seconds when the step allows them.
        if (!DateTime.TryParseExact(text, [InputFormat, InputFormat + ":ss"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
            throw new ShiftFormException(problem);
        return ZoneTime.ToUtc(local, Zone);
    }
}

/// <summary>A form that cannot be sent; the message is a key of the module's texts.</summary>
public sealed class ShiftFormException(string message) : Exception(message);
