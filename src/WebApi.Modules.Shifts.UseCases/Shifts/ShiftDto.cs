using soft.square.net.WebApi.Modules.Shifts.UseCases.Attendance;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;

/// <summary>
/// A shift as callers see it: its section, clock, state, grant source and rota with everyone's attendance, its
/// Organization unit (<paramref name="UnitId"/>; null for a shift planned before its section moved to Organization),
/// the lead's handover note, and the attendance thresholds that hold for it. A shift cloned from a template of a unit
/// with no section has no <paramref name="SectionId"/> (<paramref name="Section"/> is then the unit's name).
/// </summary>
/// <param name="CheckedOutAt">When the shift ended (checked out as a whole, or by the clock); null while it has not.</param>
/// <param name="CheckInOpensAt">The start minus the early check-in threshold.</param>
public sealed record ShiftDto(int Id, int? SectionId, string Section, IReadOnlyList<string> Categories, DateTimeOffset StartsAt, DateTimeOffset EndsAt,
    bool Published, bool Running, DateTimeOffset? CheckedOutAt, string? CheckedOutBy, string GrantSource, IReadOnlyList<RotaMemberDto> Rota,
    Guid? UnitId, bool Ended, DateTimeOffset CheckInOpensAt, string? HandoverNote, string? HandoverBy, DateTimeOffset? HandoverAt,
    AttendanceThresholdsDto Thresholds, DateTimeOffset? EndedAt, string? EndedBy);

/// <summary>
/// One member of the rota: their role's key, whether it is the shift's lead, their attendance (<paramref name="Status"/>
/// is one of <c>expected</c>, <c>present</c>, <c>late</c>, <c>absent</c>, <c>left-early</c>, <c>overtime</c>), and their
/// grant window: from their check-in to their check-out, or to the end plus the grace while they are in (null before
/// they check in: nobody has rights from a shift without checking in).
/// </summary>
public sealed record RotaMemberDto(string UserId, string RoleKey, bool IsLead, string Status, DateTimeOffset? CheckedInAt, string? CheckedInBy,
    DateTimeOffset? CheckedOutAt, string? CheckedOutBy, bool CheckedOutAutomatically, DateTimeOffset? AbsentAt, DateTimeOffset? AccessFrom,
    DateTimeOffset? AccessEnds);

/// <summary>One entry of a planned rota: a person and the key of a role definition that holds for the shift's unit.</summary>
public sealed record RotaEntry(string UserId, string RoleKey);

/// <summary>The shift's clock and its section's effective timers, for the workflow service.</summary>
public sealed record ShiftAutomationInfo(int ShiftId, int? SectionId, DateTimeOffset StartsAt, DateTimeOffset EndsAt,
    DateTimeOffset? CheckedOutAt, TimeSpan AutoAssignDelay, TimeSpan CheckoutGrace);
