namespace soft.square.net.WebApi.Modules.Shifts.Core.TemplateAggregate;

/// <summary>
/// A unit's recurring shift (table <c>shift_templates</c>): it starts at <see cref="StartLocal"/>, wall-clock time in
/// the unit's time zone, and lasts <see cref="Duration"/> (elapsed time, at most 24 hours). Its slots say how many people
/// each role needs; its default members are the rota a clone starts with.
/// </summary>
public sealed class ShiftTemplate : AggregateRoot<int>, ITenantOwned
{
    public const int NameLength = 200;
    public const int MaxSlotCount = 100;

    private readonly List<ShiftTemplateSlot> _slots = [];
    private readonly List<ShiftTemplateMember> _defaultMembers = [];

    private ShiftTemplate() { } // EF Core

    public string TenantId { get; set; } = "";
    public Guid UnitId { get; private set; }
    public string Name { get; private set; } = "";
    /// <summary>When the shift starts, as wall-clock time of day in the unit's time zone.</summary>
    public TimeOnly StartLocal { get; private set; }
    /// <summary>How long the shift lasts (elapsed time, so a night across a clock change still lasts this long).</summary>
    public TimeSpan Duration { get; private set; }

    public IReadOnlyList<ShiftTemplateSlot> Slots => _slots;
    public IReadOnlyList<ShiftTemplateMember> DefaultMembers => _defaultMembers;

    public static ShiftTemplate Create(Guid unitId, string name, TimeOnly startLocal, TimeSpan duration,
        IEnumerable<(string RoleKey, int Count)> slots, IEnumerable<(string UserId, string RoleKey)> defaultMembers)
    {
        if (unitId == Guid.Empty) throw new DomainException("A template belongs to a unit.");
        var template = new ShiftTemplate { UnitId = unitId };
        template.Update(name, startLocal, duration, slots, defaultMembers);
        return template;
    }

    public void Update(string name, TimeOnly startLocal, TimeSpan duration,
        IEnumerable<(string RoleKey, int Count)> slots, IEnumerable<(string UserId, string RoleKey)> defaultMembers)
    {
        var trimmed = (name ?? "").Trim();
        if (trimmed.Length == 0) throw new DomainException("A template needs a name.");
        if (trimmed.Length > NameLength) throw new DomainException($"A template's name has at most {NameLength} characters.");
        if (duration <= TimeSpan.Zero) throw new DomainException("A shift lasts some time.");
        if (duration > TimeSpan.FromHours(24)) throw new DomainException("A shift lasts at most 24 hours.");

        var slotList = slots.Select(s => (RoleKey: (s.RoleKey ?? "").Trim(), s.Count)).ToArray();
        if (slotList.Any(s => s.RoleKey.Length == 0)) throw new DomainException("Each slot names a role.");
        if (slotList.Any(s => s.Count is < 1 or > MaxSlotCount)) throw new DomainException($"A slot needs 1 to {MaxSlotCount} people.");
        if (slotList.GroupBy(s => s.RoleKey).Any(g => g.Count() > 1)) throw new DomainException("Each role has one slot.");

        var memberList = defaultMembers
            .Where(m => !string.IsNullOrWhiteSpace(m.UserId) && !string.IsNullOrWhiteSpace(m.RoleKey))
            .Select(m => (UserId: m.UserId.Trim(), RoleKey: m.RoleKey.Trim())).Distinct().ToArray();

        Name = trimmed;
        StartLocal = startLocal;
        Duration = duration;
        _slots.Clear();
        _slots.AddRange(slotList.Select(s => new ShiftTemplateSlot(s.RoleKey, s.Count)));
        _defaultMembers.Clear();
        _defaultMembers.AddRange(memberList.Select(m => new ShiftTemplateMember(m.UserId, m.RoleKey)));
    }

    /// <summary>
    /// The UTC window of the shift on <paramref name="date"/> (a calendar day of <paramref name="zone"/>). The start is
    /// <see cref="StartLocal"/> on that day in the zone, safe across daylight-saving changes:
    /// <list type="bullet">
    /// <item>a start the clocks skip (spring forward) moves forward by the gap, as the clocks do (02:30 becomes 03:30);</item>
    /// <item>a start the clocks pass twice (fall back) is the first of the two;</item>
    /// </list>
    /// and the end is <see cref="Duration"/> later.
    /// </summary>
    public (DateTimeOffset StartsAt, DateTimeOffset EndsAt) WindowOn(DateOnly date, TimeZoneInfo zone)
    {
        var startsAt = ToUtc(date.ToDateTime(StartLocal, DateTimeKind.Unspecified), zone);
        return (startsAt, startsAt + Duration);
    }

    /// <summary>A wall-clock time of <paramref name="zone"/> as a UTC instant (see <see cref="WindowOn"/>).</summary>
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
}

/// <summary>How many people a template's shift needs in one role.</summary>
public sealed class ShiftTemplateSlot : EntityBase<int>
{
    private ShiftTemplateSlot() { } // EF Core

    public ShiftTemplateSlot(string roleKey, int count)
    {
        RoleKey = roleKey;
        Count = count;
    }

    public string RoleKey { get; private set; } = "";
    public int Count { get; private set; }
}

/// <summary>A person a clone of the template puts on the rota, in a role.</summary>
public sealed class ShiftTemplateMember : EntityBase<int>
{
    private ShiftTemplateMember() { } // EF Core

    public ShiftTemplateMember(string userId, string roleKey)
    {
        UserId = userId;
        RoleKey = roleKey;
    }

    public string UserId { get; private set; } = "";
    public string RoleKey { get; private set; } = "";
}
