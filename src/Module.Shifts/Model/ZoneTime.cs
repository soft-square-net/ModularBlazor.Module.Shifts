using System.Globalization;

namespace soft.square.net.Module.Shifts.Model;

/// <summary>
/// The wall clock of a unit's time zone: the zone itself, local times as instants (the same rules as the API's template
/// clone across daylight-saving changes) and the week the planner shows.
/// </summary>
public static class ZoneTime
{
    /// <summary>
    /// The zone of an IANA id; <paramref name="fallback"/> (UTC unless given) with <c>Known</c> false when there is no id,
    /// or the browser does not know it.
    /// </summary>
    public static (TimeZoneInfo Zone, bool Known) Find(string? id, TimeZoneInfo? fallback = null)
    {
        if (!string.IsNullOrWhiteSpace(id) && TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone)) return (zone, true);
        return (fallback ?? TimeZoneInfo.Utc, false);
    }

    /// <summary>
    /// A wall-clock time of <paramref name="zone"/> as a UTC instant: a time the clocks skip moves forward by the gap; a
    /// time they pass twice is the first of the two.
    /// </summary>
    public static DateTimeOffset ToUtc(DateTime local, TimeZoneInfo zone)
    {
        local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        TimeSpan offset;
        if (zone.IsInvalidTime(local))
            offset = zone.GetUtcOffset(local.AddHours(-6)); // the offset before the gap: the instant lands after it
        else if (zone.IsAmbiguousTime(local))
            offset = zone.GetAmbiguousTimeOffsets(local).Max(); // the larger offset is the earlier instant
        else
            offset = zone.GetUtcOffset(local);
        return new DateTimeOffset(local.Ticks - offset.Ticks, TimeSpan.Zero);
    }

    public static DateTime Local(DateTimeOffset at, TimeZoneInfo zone) => TimeZoneInfo.ConvertTime(at, zone).DateTime;

    public static DateOnly DateOf(DateTimeOffset at, TimeZoneInfo zone) => DateOnly.FromDateTime(Local(at, zone));

    /// <summary>The first day of the week that holds <paramref name="day"/>.</summary>
    public static DateOnly WeekStart(DateOnly day, DayOfWeek firstDay) => day.AddDays(-(((int)day.DayOfWeek - (int)firstDay + 7) % 7));

    /// <summary>The week from local midnight of <paramref name="start"/> to local midnight seven days later, as instants.</summary>
    public static (DateTimeOffset From, DateTimeOffset To) Week(DateOnly start, TimeZoneInfo zone) =>
        (ToUtc(start.ToDateTime(TimeOnly.MinValue), zone), ToUtc(start.AddDays(7).ToDateTime(TimeOnly.MinValue), zone));

    // Numbers and the Gregorian calendar in every UI language: an Arabic culture may otherwise print Hijri years.
    public static string Date(DateOnly day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static string Stamp(DateTimeOffset at, TimeZoneInfo zone) => Local(at, zone).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    /// <summary>"09:00 – 17:00", with the end's date when it is another day.</summary>
    public static string Range(DateTimeOffset from, DateTimeOffset to, TimeZoneInfo zone)
    {
        var (start, end) = (Local(from, zone), Local(to, zone));
        var endText = end.ToString(start.Date == end.Date ? "HH:mm" : "MM-dd HH:mm", CultureInfo.InvariantCulture);
        return $"{start.ToString("HH:mm", CultureInfo.InvariantCulture)} – {endText}";
    }
}
