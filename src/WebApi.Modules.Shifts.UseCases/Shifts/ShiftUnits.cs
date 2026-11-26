using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Units;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;

/// <summary>The Organization unit of a shift: the one named, or the unit its section moved to. It must be cached.</summary>
public sealed class ShiftUnits(IReadRepository<SectionUnitMap> maps, UnitCache units)
{
    public async Task<Result<Guid>> ResolveAsync(int sectionId, Guid? requested, CancellationToken ct)
    {
        var moved = await maps.FirstOrDefaultAsync(new SectionUnitMapsSpec(sectionId), ct);
        if (requested is { } unitId && moved is not null && moved.UnitId != unitId)
            return Result.Invalid(new ValidationError(nameof(requested), $"Section {sectionId} moved to unit {moved.UnitId}: plan its shifts there."));

        var id = requested ?? moved?.UnitId;
        if (id is null)
            return Result.Invalid(new ValidationError("unitId",
                $"A shift needs an Organization unit: name one (unitId), or move section {sectionId} to Organization first."));
        if (await units.FindAsync(id.Value, ct) is null)
            return Result.Invalid(new ValidationError("unitId",
                $"Unit {id} is not known here. Pick a unit of the tenant's Organization tree, or resync the units (POST /units/resync)."));
        return id.Value;
    }
}
