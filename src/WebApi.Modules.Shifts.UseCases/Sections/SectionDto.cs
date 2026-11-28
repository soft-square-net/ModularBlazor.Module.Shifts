using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Sections;

/// <summary>
/// A section: its kind, its categories (none for a department, which serves them all), its optional overrides of the
/// module's settings (minutes) and the Organization unit it moved to (null while it has not moved). Sections are
/// read-only: new ones are Section units in Organization.
/// </summary>
public sealed record SectionDto(int Id, string Name, SectionKind Kind, IReadOnlyList<string> Categories, int? AutoAssignDelayMinutes, int? CheckoutGraceMinutes,
    Guid? MovedToUnitId = null)
{
    public static SectionDto From(Section s, Guid? movedToUnitId = null) =>
        new(s.Id, s.Name, s.Kind, s.Categories.Select(c => c.CategoryId).ToArray(), s.AutoAssignDelayMinutes, s.CheckoutGraceMinutes, movedToUnitId);

    public static Result<T> NotFound<T>(int id) => Result<T>.NotFound($"Section {id} was not found.");
}
