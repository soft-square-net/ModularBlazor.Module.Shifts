using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Sections;

/// <summary>
/// A section: its kind, its categories (none for a department, which serves them all) and its optional overrides of the
/// module's settings (minutes).
/// </summary>
public sealed record SectionDto(int Id, string Name, SectionKind Kind, IReadOnlyList<string> Categories, int? AutoAssignDelayMinutes, int? CheckoutGraceMinutes)
{
    public static SectionDto From(Section s) =>
        new(s.Id, s.Name, s.Kind, s.Categories.Select(c => c.CategoryId).ToArray(), s.AutoAssignDelayMinutes, s.CheckoutGraceMinutes);

    public static Result<T> NotFound<T>(int id) => Result<T>.NotFound($"Section {id} was not found.");
}
