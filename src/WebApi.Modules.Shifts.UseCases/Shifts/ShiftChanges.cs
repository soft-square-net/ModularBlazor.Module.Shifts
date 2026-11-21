using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Sections;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;

/// <summary>
/// The shape of every change to a shift: load it, apply the change (a rights check, then the aggregate's method),
/// save, rewrite its grants when it is published, and return it as the caller now sees it.
/// </summary>
public sealed class ShiftChanges(IRepository<Shift> shifts, IReadRepository<Section> sections, ShiftGrants grants, ShiftDetails details)
{
    public async Task<Result<ShiftDto>> ChangeAsync(int id, Func<Shift, Task<Result>> change, CancellationToken ct)
    {
        var shift = await shifts.FirstOrDefaultAsync(new ShiftByIdSpec(id), ct);
        if (shift is null) return ShiftDetails.NotFound<ShiftDto>(id);
        var changed = await change(shift);
        if (!changed.IsSuccess) return changed.As<ShiftDto>();

        await shifts.UpdateAsync(shift, ct);
        var section = await sections.FirstOrDefaultAsync(new SectionByIdSpec(shift.SectionId), ct);
        if (section is null) return SectionDto.NotFound<ShiftDto>(shift.SectionId);
        await grants.WriteAsync(shift, section, ct);
        return (await details.ToDtosAsync([shift], ct))[0];
    }

    public Task<Result<ShiftDto>> ChangeAsync(int id, Action<Shift> change, CancellationToken ct) =>
        ChangeAsync(id, shift => { change(shift); return Task.FromResult(Result.Success()); }, ct);
}
