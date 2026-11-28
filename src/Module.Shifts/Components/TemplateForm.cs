using System.Globalization;
using soft.square.net.Module.Shifts.Model;
using soft.square.net.WebApi.Modules.Shifts.Client;

namespace soft.square.net.Module.Shifts.Components;

/// <summary>What the template editor edits: a name, a start on the unit's clock, a duration, slots and default members.</summary>
public sealed class TemplateForm
{
    /// <summary>Null for a new template.</summary>
    public int? Id { get; init; }

    public Guid UnitId { get; init; }
    public string Name { get; set; } = "";

    /// <summary>The time input's own text ("HH:mm").</summary>
    public string Start { get; set; } = "09:00";

    public int Hours { get; set; } = 8;
    public int Minutes { get; set; }
    public List<SlotRow> Slots { get; set; } = [];
    public List<ShiftForm.RotaRow> Members { get; set; } = [];

    public sealed class SlotRow
    {
        public string RoleKey { get; set; } = "desk";
        public int Count { get; set; } = 1;
    }

    public static TemplateForm New(Guid unitId) => new()
    {
        UnitId = unitId,
        Slots = [new SlotRow { RoleKey = "manager" }, new SlotRow { RoleKey = "desk" }],
    };

    public static TemplateForm From(ShiftTemplateDto template)
    {
        var duration = ParseDuration(template.Duration);
        return new TemplateForm
        {
            Id = template.Id,
            UnitId = template.UnitId,
            Name = template.Name,
            Start = FormatStart(template.StartLocal),
            Hours = (int)duration.TotalHours,
            Minutes = duration.Minutes,
            Slots = template.Slots.Select(s => new SlotRow { RoleKey = s.RoleKey, Count = s.Count }).ToList(),
            Members = template.DefaultMembers.Select(m => new ShiftForm.RotaRow { UserId = m.UserId, RoleKey = m.RoleKey }).ToList(),
        };
    }

    /// <summary>The API writes a duration as "hh:mm:ss", or "d.hh:mm:ss" for a whole day.</summary>
    public static TimeSpan ParseDuration(string? text) =>
        TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out var duration) ? duration : TimeSpan.Zero;

    public static string FormatStart(TimeSpan start) => start.ToString(@"hh\:mm", CultureInfo.InvariantCulture);

    /// <summary>The request, or why the form cannot become one (a text of the module, to translate).</summary>
    public TemplateRequest ToRequest()
    {
        if (string.IsNullOrWhiteSpace(Name)) throw new ShiftFormException("Enter the template's name.");
        if (!TimeOnly.TryParseExact(Start, ["HH:mm", "HH:mm:ss"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var start))
            throw new ShiftFormException("Enter the template's start as a time.");
        var duration = new TimeSpan(Hours, Minutes, 0);
        if (Hours < 0 || Minutes is < 0 or > 59 || duration <= TimeSpan.Zero || duration > TimeSpan.FromHours(24))
            throw new ShiftFormException("A template lasts more than nothing and at most 24 hours.");
        return new TemplateRequest
        {
            UnitId = UnitId,
            Name = Name.Trim(),
            StartLocal = start.ToTimeSpan(),
            Duration = duration.ToString("c", CultureInfo.InvariantCulture),
            Slots = Slots.Where(s => s.Count > 0).Select(s => new TemplateSlotDto { RoleKey = s.RoleKey, Count = s.Count }).ToList(),
            DefaultMembers = Members.Where(m => !string.IsNullOrWhiteSpace(m.UserId))
                .Select(m => new TemplateMemberDto { UserId = m.UserId.Trim(), RoleKey = m.RoleKey }).ToList(),
        };
    }
}
