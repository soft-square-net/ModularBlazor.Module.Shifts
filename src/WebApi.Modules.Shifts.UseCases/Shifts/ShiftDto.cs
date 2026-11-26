using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;

/// <summary>
/// A shift as callers see it: its section, clock, state, grant source and rota, and its Organization unit
/// (<paramref name="UnitId"/>; null for a shift planned before its section moved to Organization).
/// </summary>
public sealed record ShiftDto(int Id, int SectionId, string Section, IReadOnlyList<string> Categories, DateTimeOffset StartsAt, DateTimeOffset EndsAt,
    bool Published, bool Running, DateTimeOffset? CheckedOutAt, string? CheckedOutBy, string GrantSource, IReadOnlyList<RotaMemberDto> Rota,
    Guid? UnitId = null);

/// <summary>One member of the rota, and when their access ends (the scheduled end, or earlier on check-out).</summary>
public sealed record RotaMemberDto(string UserId, ShiftRole Role, DateTimeOffset? CheckedOutAt, DateTimeOffset AccessEnds);

/// <summary>One entry of a planned rota: a person and a role.</summary>
public sealed record RotaEntry(string UserId, ShiftRole Role);

/// <summary>The shift's clock and its section's effective timers, for the workflow service.</summary>
public sealed record ShiftAutomationInfo(int ShiftId, int SectionId, DateTimeOffset StartsAt, DateTimeOffset EndsAt,
    DateTimeOffset? CheckedOutAt, TimeSpan AutoAssignDelay, TimeSpan CheckoutGrace);
