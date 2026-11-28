using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Units;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;

/// <summary>
/// The Organization unit of a shift, during the move to Organization:
/// <list type="bullet">
/// <item>a section that has not moved yet: the unit named (it must be cached), or none (null, as before the move), so
/// today's client keeps planning shifts;</item>
/// <item>a moved section: its unit by default; naming another unit is refused;</item>
/// <item>no section (planned for a unit only): the unit named, which is required.</item>
/// </list>
/// </summary>
public sealed class ShiftUnits(IReadRepository<SectionUnitMap> maps, UnitCache units)
{
    public async Task<Result<Guid?>> ResolveAsync(int? sectionId, Guid? requested, CancellationToken ct)
    {
        if (sectionId is null && requested is null)
            return Result<Guid?>.Invalid(new ValidationError("unitId", "A shift belongs to a unit: pick one (or, before the move to Organization, a section)."));
        var moved = sectionId is { } section ? await maps.FirstOrDefaultAsync(new SectionUnitMapsSpec(section), ct) : null;
        if (moved is not null && requested is { } other && other != moved.UnitId)
            return Result<Guid?>.Invalid(new ValidationError("unitId", $"Section {sectionId} moved to unit {moved.UnitId}: plan its shifts there."));

        var id = requested ?? moved?.UnitId;
        if (id is null) return Result<Guid?>.Success(null); // a section that has not moved: no unit yet
        if (await units.FindAsync(id.Value, ct) is null)
            return Result<Guid?>.Invalid(new ValidationError("unitId",
                $"Unit {id} is not known here. Pick a unit of the tenant's Organization tree, or resync the units (POST /units/resync)."));
        return id;
    }

    /// <summary>The section that moved to this unit, if any: a shift planned for the unit keeps it (as a clone does).</summary>
    public async Task<int?> SectionOfAsync(Guid unitId, CancellationToken ct) =>
        (await maps.ListAsync(new SectionUnitMapByUnitSpec(unitId), ct)).FirstOrDefault()?.SectionId;
}
