namespace soft.square.net.WebApi.Modules.Shifts.UseCases;

/// <summary>
/// The integration events this module publishes (v1). The data of a published version never changes: a new shape is a
/// <c>v2</c> next to it. Subscribers copy the record (Content does), until the <c>Shifts.Contracts</c> package ships it.
/// </summary>
public static class ShiftsEvents
{
    /// <summary>
    /// A section moved to Organization (<see cref="SectionMoved"/>). Subject <c>section:{sectionId}</c>; the envelope's
    /// UnitPath is the unit's path and its OrgId the org id (32-digit form).
    /// </summary>
    public const string SectionMoved = "shifts.section-moved.v1";

    public static string SectionSubject(int sectionId) => $"section:{sectionId}";
}

/// <summary>Data of <see cref="ShiftsEvents.SectionMoved"/>: the Shifts section <paramref name="SectionId"/> is now the Organization unit <paramref name="UnitId"/>.</summary>
public sealed record SectionMoved(int SectionId, Guid UnitId);
