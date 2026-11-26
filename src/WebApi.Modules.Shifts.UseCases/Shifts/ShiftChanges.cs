using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Sections;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;

/// <summary>What a change sees: the shift and its section (null for a shift with no section).</summary>
public sealed record ShiftChange(Shift Shift, Section? Section);

/// <summary>
/// The shape of every change to a shift: load it and its section, apply the change (a rights check, the aggregate's
/// method, and the change's events through <see cref="ShiftNotices"/>), save (the outbox rows go in the same save),
/// rewrite its grants when it is published, send the live nudge, and return it as the caller now sees it.
/// </summary>
public sealed class ShiftChanges(IRepository<Shift> shifts, IReadRepository<Section> sections, ShiftGrants grants, ShiftDetails details,
    ShiftNotices notices)
{
    public async Task<Result<ShiftDto>> ChangeWithSectionAsync(int id, Func<ShiftChange, Task<Result>> change, CancellationToken ct)
    {
        var shift = await shifts.FirstOrDefaultAsync(new ShiftByIdSpec(id), ct);
        if (shift is null) return ShiftDetails.NotFound<ShiftDto>(id);
        Section? section = null;
        if (shift.SectionId is { } sectionId)
        {
            section = await sections.FirstOrDefaultAsync(new SectionByIdSpec(sectionId), ct);
            if (section is null) return SectionDto.NotFound<ShiftDto>(sectionId);
        }

        var changed = await change(new ShiftChange(shift, section));
        if (!changed.IsSuccess) return changed.As<ShiftDto>();

        await shifts.UpdateAsync(shift, ct);
        await grants.WriteAsync(shift, section, ct);
        await notices.NudgeAsync(shift, ct);
        return (await details.ToDtosAsync([shift], ct))[0];
    }

    public Task<Result<ShiftDto>> ChangeAsync(int id, Func<Shift, Task<Result>> change, CancellationToken ct) =>
        ChangeWithSectionAsync(id, c => change(c.Shift), ct);

    public Task<Result<ShiftDto>> ChangeAsync(int id, Action<Shift> change, CancellationToken ct) =>
        ChangeWithSectionAsync(id, c => { change(c.Shift); return Task.FromResult(Result.Success()); }, ct);
}
