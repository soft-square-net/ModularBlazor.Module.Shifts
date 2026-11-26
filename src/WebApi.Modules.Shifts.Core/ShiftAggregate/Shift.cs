namespace soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;

/// <summary>
/// A shift of one unit (or, before the move to Organization, of one section) on the clock (UTC) with its rota. Once published, every member's grants are written from
/// it (shifts module → <c>IGrantStore</c>, source <c>shift:{id}</c>); they are valid from the start to the member's
/// effective end: the scheduled end, or earlier when the member or the whole shift checked out.
/// </summary>
public sealed class Shift : AggregateRoot<int>, ITenantOwned
{
    private readonly List<ShiftMember> _members = [];

    private Shift() { } // EF Core

    public string TenantId { get; set; } = "";
    /// <summary>The Shifts section of a shift planned before the move to Organization; null for one cloned from a template of a unit with no section.</summary>
    public int? SectionId { get; private set; }
    /// <summary>
    /// The Organization unit the shift belongs to (a cached unit). Set for every new shift, and for the existing ones
    /// when their section moves to Organization; it replaces <see cref="SectionId"/> once the move is done.
    /// </summary>
    public Guid? UnitId { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public bool Published { get; private set; }
    public DateTimeOffset? CheckedOutAt { get; private set; }
    public string? CheckedOutBy { get; private set; }

    public IReadOnlyList<ShiftMember> Members => _members;

    /// <summary>The grant source of this shift's rota.</summary>
    public string GrantSource => $"shift:{Id}";

    public static Shift Create(int? sectionId, DateTimeOffset startsAt, DateTimeOffset endsAt, IEnumerable<RotaAssignment> rota,
        Guid? unitId = null)
    {
        if (unitId == Guid.Empty) throw new DomainException("A shift's unit cannot be empty.");
        if (sectionId is null && unitId is null) throw new DomainException("A shift belongs to a unit (or to a section).");
        var shift = new Shift { SectionId = sectionId, UnitId = unitId };
        shift.Reschedule(startsAt, endsAt);
        shift.SetRota(rota);
        return shift;
    }

    public void Reschedule(DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        if (CheckedOutAt is not null) throw new DomainException("The shift is checked out.");
        startsAt = startsAt.ToUniversalTime();
        endsAt = endsAt.ToUniversalTime();
        if (endsAt <= startsAt) throw new DomainException("A shift ends after it starts.");
        if (endsAt - startsAt > TimeSpan.FromHours(24)) throw new DomainException("A shift lasts at most 24 hours.");
        StartsAt = startsAt;
        EndsAt = endsAt;
    }

    /// <summary>
    /// Replaces the rota. A person may hold several roles. A shift has at most one lead (a member in a lead role), and
    /// exactly one once it is published: a draft (a template's clone) may wait for its lead.
    /// </summary>
    public void SetRota(IEnumerable<RotaAssignment> rota)
    {
        if (CheckedOutAt is not null) throw new DomainException("The shift is checked out.");
        var entries = rota.Where(r => !string.IsNullOrWhiteSpace(r.UserId) && !string.IsNullOrWhiteSpace(r.RoleKey))
            .Select(r => r with { UserId = r.UserId.Trim(), RoleKey = r.RoleKey.Trim() })
            .DistinctBy(r => (r.UserId, r.RoleKey)).ToArray();
        CheckLeads(entries.Count(e => e.IsLead), Published);
        _members.RemoveAll(m => !entries.Any(e => e.UserId == m.UserId && e.RoleKey == m.RoleKey && e.IsLead == m.IsLead));
        foreach (var entry in entries.Where(e => !_members.Any(m => m.UserId == e.UserId && m.RoleKey == e.RoleKey)))
            _members.Add(new ShiftMember(entry.UserId, entry.RoleKey, entry.IsLead));
    }

    /// <summary>The rota as assignments (to re-check it, or to rebuild it).</summary>
    public IReadOnlyList<RotaAssignment> Rota => [.. _members.Select(m => new RotaAssignment(m.UserId, m.RoleKey, m.IsLead))];

    private static void CheckLeads(int leads, bool published)
    {
        if (leads > 1) throw new DomainException("A shift has exactly one lead.");
        if (published && leads == 0) throw new DomainException("A published shift has exactly one lead: put someone in a lead role (such as manager) on the rota.");
    }

    /// <summary>The shift's section moved to Organization: the shift now belongs to the section's unit.</summary>
    public void MoveToUnit(Guid unitId)
    {
        if (unitId == Guid.Empty) throw new DomainException("A shift's unit cannot be empty.");
        UnitId = unitId;
    }

    /// <summary>Publishes the shift: it needs exactly one lead.</summary>
    public void Publish()
    {
        CheckLeads(_members.Count(m => m.IsLead), published: true);
        Published = true;
    }

    public bool IsRunning(DateTimeOffset now) => CheckedOutAt is null && StartsAt <= now && now < EndsAt;

    /// <summary>True when <paramref name="userId"/> is the shift's lead.</summary>
    public bool IsLead(string userId) => _members.Any(m => m.UserId == userId && m.IsLead);

    /// <summary>One member leaves early: their grants end now.</summary>
    public void CheckOutMember(string userId, DateTimeOffset now)
    {
        var members = _members.Where(m => m.UserId == userId && m.CheckedOutAt is null).ToArray();
        if (members.Length == 0) throw new DomainException($"'{userId}' is not on this shift, or has checked out already.");
        if (now < StartsAt) throw new DomainException("The shift has not started yet.");
        foreach (var member in members) member.CheckOut(now < EndsAt ? now : EndsAt);
    }

    /// <summary>The whole shift checks out (the lead at handover, or the workflow after the grace period): every grant ends now.</summary>
    public void CheckOut(string actor, DateTimeOffset now)
    {
        if (CheckedOutAt is not null) throw new DomainException("The shift is checked out already.");
        if (now < StartsAt) throw new DomainException("The shift has not started yet.");
        CheckedOutAt = now < EndsAt ? now : EndsAt;
        CheckedOutBy = actor;
    }

    /// <summary>When a member's access ends: the scheduled end, or earlier on check-out.</summary>
    public DateTimeOffset EndOf(ShiftMember member) =>
        new[] { EndsAt, member.CheckedOutAt ?? EndsAt, CheckedOutAt ?? EndsAt }.Min();
}
