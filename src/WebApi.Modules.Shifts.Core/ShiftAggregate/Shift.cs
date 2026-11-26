namespace soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;

/// <summary>
/// A shift of one unit (or, before the move to Organization, of one section) on the clock (UTC) with its rota. Once it
/// is published its members check in; each checked-in member's grants are written from it (shifts module →
/// <c>IGrantStore</c>, source <c>shift:{id}</c>), valid from their check-in to their check-out, or to the end plus the
/// check-out grace (<see cref="ShiftMember.AccessOn"/>). Nobody has rights from a shift without checking in.
/// </summary>
public sealed class Shift : AggregateRoot<int>, ITenantOwned
{
    public const int HandoverNoteLength = 4000;

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
    /// <summary>When the shift ended (see <see cref="Ended"/>): checked out as a whole, or by the clock.</summary>
    public DateTimeOffset? CheckedOutAt { get; private set; }
    public string? CheckedOutBy { get; private set; }
    /// <summary>When the shift actually ended: the time of the check-out of the whole shift, or of the clock's tick that ended it.</summary>
    public DateTimeOffset? EndedAt { get; private set; }
    /// <summary>Who ended it: the user who checked the whole shift out, or the clock's actor.</summary>
    public string? EndedBy { get; private set; }
    /// <summary>The lead's handover note (<see cref="WriteHandover"/>).</summary>
    public string? HandoverNote { get; private set; }
    public string? HandoverBy { get; private set; }
    public DateTimeOffset? HandoverAt { get; private set; }

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
        if (Ended) throw new DomainException("The shift is checked out.");
        startsAt = startsAt.ToUniversalTime();
        endsAt = endsAt.ToUniversalTime();
        if (endsAt <= startsAt) throw new DomainException("A shift ends after it starts.");
        if (endsAt - startsAt > TimeSpan.FromHours(24)) throw new DomainException("A shift lasts at most 24 hours.");
        StartsAt = startsAt;
        EndsAt = endsAt;
    }

    /// <summary>
    /// Replaces the rota. A person may hold several roles. A shift has at most one lead (a member in a lead role), and
    /// exactly one once it is published: a draft (a template's clone) may wait for its lead. Members who stay keep their
    /// attendance; a person in a new role of theirs is checked in already when they are.
    /// </summary>
    public void SetRota(IEnumerable<RotaAssignment> rota)
    {
        if (Ended) throw new DomainException("The shift is checked out.");
        var entries = rota.Where(r => !string.IsNullOrWhiteSpace(r.UserId) && !string.IsNullOrWhiteSpace(r.RoleKey))
            .Select(r => r with { UserId = r.UserId.Trim(), RoleKey = r.RoleKey.Trim() })
            .DistinctBy(r => (r.UserId, r.RoleKey)).ToArray();
        CheckLeads(entries.Count(e => e.IsLead), Published);
        _members.RemoveAll(m => !entries.Any(e => e.UserId == m.UserId && e.RoleKey == m.RoleKey && e.IsLead == m.IsLead));
        foreach (var entry in entries.Where(e => !_members.Any(m => m.UserId == e.UserId && m.RoleKey == e.RoleKey)))
        {
            var member = new ShiftMember(entry.UserId, entry.RoleKey, entry.IsLead);
            if (_members.FirstOrDefault(m => m.UserId == entry.UserId) is { } same) member.CopyAttendance(same);
            _members.Add(member);
        }
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

    /// <summary>Publishes the shift: it needs exactly one lead. Its members may then check in.</summary>
    public void Publish()
    {
        CheckLeads(_members.Count(m => m.IsLead), published: true);
        Published = true;
    }

    public bool IsRunning(DateTimeOffset now) => CheckedOutAt is null && StartsAt <= now && now < EndsAt;

    /// <summary>
    /// The shift has ended: checked out as a whole (the lead at handover, or a manager), or by the clock at the end plus
    /// the grace. Nothing changes on it any more, except the handover note.
    /// </summary>
    public bool Ended => CheckedOutAt is not null;

    /// <summary>True when <paramref name="userId"/> is the shift's lead.</summary>
    public bool IsLead(string userId) => _members.Any(m => m.UserId == userId && m.IsLead);

    /// <summary>True when <paramref name="userId"/> is on the rota (in any role).</summary>
    public bool HasMember(string userId) => _members.Any(m => m.UserId == userId);

    /// <summary>
    /// <paramref name="userId"/> checks in (all of their roles), themselves or through <paramref name="by"/>: from
    /// <see cref="AttendanceThresholds.EarlyCheckIn"/> before the start until the end, on a published shift that has not
    /// ended. A member who checked out earlier may check in again before the end; their grants then run on from their
    /// first check-in. Throws when they are in already.
    /// </summary>
    public void CheckIn(string userId, string by, DateTimeOffset now, AttendanceThresholds thresholds)
    {
        var members = _members.Where(m => m.UserId == userId).ToArray();
        if (members.Length == 0) throw new DomainException($"'{userId}' is not on this shift.");
        if (!Published) throw new DomainException("The shift is not published yet.");
        if (Ended) throw new DomainException("The shift has ended.");
        if (now < StartsAt - thresholds.EarlyCheckIn)
            throw new DomainException($"Check-in opens {thresholds.EarlyCheckIn.TotalMinutes:0} minutes before the shift starts.");
        if (now >= EndsAt) throw new DomainException("The shift is over: check-in has closed.");
        if (members.All(m => m.IsIn)) throw new DomainException($"'{userId}' has checked in already.");
        foreach (var member in members)
        {
            if (member.CheckedInAt is null) member.CheckIn(now, by);
            else member.Resume();
        }
    }

    /// <summary>One member checks out (themselves, the lead or a manager for them): their grants end now.</summary>
    public void CheckOutMember(string userId, string by, DateTimeOffset now)
    {
        if (!HasMember(userId)) throw new DomainException($"'{userId}' is not on this shift.");
        var members = _members.Where(m => m.UserId == userId && m.IsIn).ToArray();
        if (members.Length == 0) throw new DomainException($"'{userId}' has not checked in, or has checked out already.");
        foreach (var member in members) member.CheckOut(now, by);
    }

    /// <summary>
    /// The whole shift checks out (the lead at handover, or a manager): everyone still in is checked out now, and the
    /// shift ends. Returns the users it checked out.
    /// </summary>
    public IReadOnlyList<string> CheckOut(string actor, DateTimeOffset now)
    {
        if (Ended) throw new DomainException("The shift is checked out already.");
        if (now < StartsAt) throw new DomainException("The shift has not started yet.");
        var checkedOut = CheckOutEveryone(now, actor, automatically: false);
        CheckedOutAt = now;
        CheckedOutBy = actor;
        EndedAt = now;
        EndedBy = actor;
        return checkedOut;
    }

    /// <summary>
    /// The clock's absence check: every member who has not checked in by the start plus
    /// <see cref="AttendanceThresholds.Absent"/> (or by the shift's end, if that is sooner) is marked absent, once.
    /// Returns the users it marked.
    /// </summary>
    public IReadOnlyList<string> MarkAbsences(DateTimeOffset now, AttendanceThresholds thresholds)
    {
        if (!Published) return [];
        var at = StartsAt + thresholds.Absent < EndsAt ? StartsAt + thresholds.Absent : EndsAt;
        if (now < at) return [];
        var absent = _members.Where(m => m.CheckedInAt is null && m.AbsentAt is null).ToArray();
        foreach (var member in absent) member.MarkAbsent(at);
        return [.. absent.Select(m => m.UserId).Distinct()];
    }

    /// <summary>
    /// The clock's end of a published shift nobody checked out: at the end plus <see cref="AttendanceThresholds.CheckoutGrace"/>
    /// everyone still in is checked out automatically at that time, the members who never came are marked absent, and
    /// the shift ends (checked out by <paramref name="actor"/>). Returns the users it checked out; null when it is not
    /// time yet, or the shift has ended already (so running it again changes nothing).
    /// </summary>
    public IReadOnlyList<string>? EndOnTheClock(string actor, DateTimeOffset now, AttendanceThresholds thresholds)
    {
        if (!Published || Ended) return null;
        var at = EndsAt + thresholds.CheckoutGrace;
        if (now < at) return null;
        MarkAbsences(now, thresholds);
        var checkedOut = CheckOutEveryone(at, actor, automatically: true);
        CheckedOutAt = at;
        CheckedOutBy = actor;
        EndedAt = now;
        EndedBy = actor;
        return checkedOut;
    }

    private string[] CheckOutEveryone(DateTimeOffset at, string actor, bool automatically)
    {
        var inside = _members.Where(m => m.IsIn).ToArray();
        foreach (var member in inside) member.CheckOut(at, actor, automatically);
        return [.. inside.Select(m => m.UserId).Distinct()];
    }

    /// <summary>The lead writes (or rewrites) the handover note, once the shift has started, and also after it ended.</summary>
    public void WriteHandover(string userId, string note, DateTimeOffset now)
    {
        if (!IsLead(userId)) throw new DomainException("Only the shift's lead writes the handover note.");
        if (!Published) throw new DomainException("The shift is not published yet.");
        if (now < StartsAt) throw new DomainException("The shift has not started yet.");
        note = note?.Trim() ?? "";
        if (note.Length == 0) throw new DomainException("A handover note needs some text.");
        if (note.Length > HandoverNoteLength) throw new DomainException($"A handover note has at most {HandoverNoteLength} characters.");
        HandoverNote = note;
        HandoverBy = userId;
        HandoverAt = now;
    }
}
