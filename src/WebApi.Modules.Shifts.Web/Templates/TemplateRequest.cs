using soft.square.net.WebApi.Modules.Shifts.UseCases.Templates;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Templates;

/// <summary>
/// The body of <see cref="Add"/> and <see cref="Update"/> (whose unit stays: its <paramref name="UnitId"/> is ignored).
/// Its name is the OpenAPI schema name: keep it stable. <paramref name="StartLocal"/>: wall-clock time of day in the
/// unit's time zone ("06:00:00"); <paramref name="Duration"/>: elapsed time, at most 24 hours ("08:00:00").
/// </summary>
public sealed record TemplateRequest(Guid UnitId, string Name, TimeOnly StartLocal, TimeSpan Duration, IReadOnlyList<TemplateSlotDto>? Slots,
    IReadOnlyList<TemplateMemberDto>? DefaultMembers);

/// <summary>The body of <see cref="Clone"/>: the calendar day, in the unit's time zone.</summary>
public sealed record TemplateCloneRequest(DateOnly Date);
