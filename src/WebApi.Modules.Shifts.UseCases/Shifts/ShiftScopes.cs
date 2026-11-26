using soft.square.net.Organization.Contracts;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.UnitAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;

/// <summary>The grant scopes of a shift's rota.</summary>
public static class ShiftScopes
{
    /// <summary>
    /// A shift of a unit: <c>unit:{id}</c>; <c>category:{c}</c> for each category of the unit (Organization's
    /// <c>CategoriesOf</c>; the wildcard <c>*</c> of a department section is skipped); and, for a department section
    /// (tag <c>department</c>), <c>section:{unitId}</c> (the unit id in the 32-digit form, as in unit scopes).
    /// The section's own scopes (<see cref="Section.Scopes"/>) are kept next to them while the shift still has its
    /// section, so Content's checks on <c>category:</c> and <c>section:{sectionId}</c> keep working until it remaps.
    /// </summary>
    public static IReadOnlyList<string> For(CachedUnit unit, IEnumerable<string> categoriesOfUnit, Section? section = null)
    {
        IEnumerable<string> scopes =
        [
            UnitScopes.Of(unit.Id),
            .. categoriesOfUnit.Where(c => !string.IsNullOrWhiteSpace(c) && c != CachedUnit.AnyCategory).Select(c => $"category:{c}"),
            .. unit.IsDepartmentSection ? [$"section:{UnitPaths.Id(unit.Id)}"] : Array.Empty<string>(),
            .. section?.Scopes ?? []
        ];
        return [.. scopes.Distinct(StringComparer.Ordinal)];
    }
}
