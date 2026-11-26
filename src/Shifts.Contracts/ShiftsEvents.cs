namespace soft.square.net.Shifts.Contracts;

/// <summary>
/// The shifts.* integration events (v1). Each is written to the module's outbox in the same save as the change it
/// reports, so it is sent once the change is committed, and never for a change that was rolled back.
/// <para>The data of a published version never changes: a new shape is a <c>v2</c> next to it. A subscriber may copy
/// these records instead of referencing this package. JSON uses the web defaults (camelCase).</para>
/// <para>The envelope: <c>Source</c> is <c>shifts</c>; <c>Subject</c> is <c>shift:{id}</c> (<see cref="ShiftSubject"/>),
/// or <c>section:{id}</c> for <see cref="SectionMoved"/>; <c>UnitPath</c> is the shift's unit path
/// (<c>/{root}/…/{unit}</c>, 32-digit ids) and <c>OrgId</c> the unit's org (32-digit form), when the shift has a
/// unit the module knows.</para>
/// </summary>
public static class ShiftsEvents
{
    /// <summary>A shift was published (<see cref="ShiftPublished"/>): its rota is planned, nobody has rights until they check in.</summary>
    public const string Published = "shifts.published.v1";

    /// <summary>A published shift was rescheduled or its rota changed (<see cref="ShiftUpdated"/>).</summary>
    public const string Updated = "shifts.updated.v1";

    /// <summary>A published shift was deleted (<see cref="ShiftCancelled"/>): every grant of it is gone.</summary>
    public const string Cancelled = "shifts.cancelled.v1";

    /// <summary>A person in a role joined the rota of a published shift (<see cref="ShiftMemberAdded"/>).</summary>
    public const string MemberAdded = "shifts.member-added.v1";

    /// <summary>A person in a role left the rota of a published shift (<see cref="ShiftMemberRemoved"/>).</summary>
    public const string MemberRemoved = "shifts.member-removed.v1";

    /// <summary>A member checked in (<see cref="ShiftCheckedIn"/>): their grants start now.</summary>
    public const string CheckedIn = "shifts.checked-in.v1";

    /// <summary>A member checked out, or was checked out by the clock (<see cref="ShiftCheckedOut"/>): their grants end now.</summary>
    public const string CheckedOut = "shifts.checked-out.v1";

    /// <summary>The lead wrote the shift's handover note (<see cref="ShiftHandover"/>).</summary>
    public const string Handover = "shifts.handover.v1";

    /// <summary>A shift ended (<see cref="ShiftEnded"/>): checked out as a whole, or by the clock after its end and grace.</summary>
    public const string Ended = "shifts.ended.v1";

    /// <summary>
    /// A section moved to Organization (<see cref="Contracts.SectionMoved"/>). Subject <c>section:{sectionId}</c>; the
    /// envelope's UnitPath is the unit's path and its OrgId the org id (32-digit form).
    /// </summary>
    public const string SectionMoved = "shifts.section-moved.v1";

    /// <summary>Every shifts event, any version (a subscription pattern).</summary>
    public const string Any = "shifts.#";

    /// <summary>Every event type of this version.</summary>
    public static IReadOnlyList<string> All { get; } =
        [Published, Updated, Cancelled, MemberAdded, MemberRemoved, CheckedIn, CheckedOut, Handover, Ended, SectionMoved];

    /// <summary>The actor of what the module's clock does on its own (absences, automatic check-outs, ends).</summary>
    public const string ClockActor = "shifts:clock";

    public static string ShiftSubject(int shiftId) => $"shift:{shiftId}";

    public static string SectionSubject(int sectionId) => $"section:{sectionId}";
}

/// <summary>
/// A shift as the events carry it, at the time of the event. <see cref="UnitId"/> and <see cref="UnitPath"/> are null for a
/// shift planned before its section moved to Organization; <see cref="SectionId"/> is null for a shift of a unit with
/// no section. Times are UTC.
/// </summary>
/// <param name="EndedAt">When the shift ended (checked out as a whole, or by the clock); null while it has not.</param>
/// <param name="HandoverNote">The lead's handover note, if any.</param>
public sealed record ShiftSnapshot(
    int ShiftId, Guid? UnitId, string? UnitPath, int? SectionId, DateTimeOffset StartsAt, DateTimeOffset EndsAt, bool Published,
    DateTimeOffset? EndedAt, string? HandoverNote, IReadOnlyList<ShiftMemberSnapshot> Members);

/// <summary>
/// One person in one role on a shift (a person in two roles is two entries), with their attendance.
/// <see cref="Status"/> is one of <see cref="AttendanceStatuses"/>.
/// </summary>
public sealed record ShiftMemberSnapshot(
    string UserId, string RoleKey, bool IsLead, DateTimeOffset? CheckedInAt, DateTimeOffset? CheckedOutAt, DateTimeOffset? AbsentAt, string Status);

/// <summary>Data of <see cref="ShiftsEvents.Published"/>.</summary>
public sealed record ShiftPublished(ShiftSnapshot Shift);

/// <summary>Data of <see cref="ShiftsEvents.Updated"/>: the shift after the change.</summary>
public sealed record ShiftUpdated(ShiftSnapshot Shift);

/// <summary>Data of <see cref="ShiftsEvents.Cancelled"/>: the shift as it was when it was deleted.</summary>
public sealed record ShiftCancelled(ShiftSnapshot Shift, string By, DateTimeOffset At);

/// <summary>Data of <see cref="ShiftsEvents.MemberAdded"/>.</summary>
public sealed record ShiftMemberAdded(int ShiftId, Guid? UnitId, string UserId, string RoleKey, bool IsLead);

/// <summary>Data of <see cref="ShiftsEvents.MemberRemoved"/>.</summary>
public sealed record ShiftMemberRemoved(int ShiftId, Guid? UnitId, string UserId, string RoleKey, bool IsLead);

/// <summary>
/// Data of <see cref="ShiftsEvents.CheckedIn"/>: <paramref name="UserId"/> checked in at <paramref name="At"/>, themselves
/// or through a manager (<paramref name="By"/>); <paramref name="Status"/> is <c>present</c> or <c>late</c>.
/// Their grants run from <paramref name="At"/> to their check-out, or to the shift's end plus the check-out grace.
/// </summary>
public sealed record ShiftCheckedIn(int ShiftId, Guid? UnitId, string UserId, IReadOnlyList<string> RoleKeys, DateTimeOffset At, string By, string Status);

/// <summary>
/// Data of <see cref="ShiftsEvents.CheckedOut"/>: <paramref name="UserId"/>'s grants of the shift end at <paramref name="At"/>.
/// <paramref name="Automatic"/>: the clock checked them out at the shift's end plus the grace (<paramref name="By"/> is then
/// <see cref="ShiftsEvents.ClockActor"/>). <paramref name="Status"/> is their attendance after it (<c>left-early</c>,
/// <c>overtime</c>, <c>present</c> or <c>late</c>).
/// </summary>
public sealed record ShiftCheckedOut(int ShiftId, Guid? UnitId, string UserId, DateTimeOffset At, string By, bool Automatic, string Status);

/// <summary>Data of <see cref="ShiftsEvents.Handover"/>: the note as the lead (<paramref name="By"/>) wrote it at <paramref name="At"/>.</summary>
public sealed record ShiftHandover(int ShiftId, Guid? UnitId, string Note, string By, DateTimeOffset At);

/// <summary>
/// Data of <see cref="ShiftsEvents.Ended"/>: the shift as it ended, with everyone's attendance (members who never checked
/// in are <c>absent</c>). <paramref name="By"/> is the user who checked the whole shift out, or
/// <see cref="ShiftsEvents.ClockActor"/>.
/// </summary>
public sealed record ShiftEnded(ShiftSnapshot Shift, string By, DateTimeOffset At);

/// <summary>Data of <see cref="ShiftsEvents.SectionMoved"/>: the Shifts section <paramref name="SectionId"/> is now the Organization unit <paramref name="UnitId"/>.</summary>
public sealed record SectionMoved(int SectionId, Guid UnitId);
