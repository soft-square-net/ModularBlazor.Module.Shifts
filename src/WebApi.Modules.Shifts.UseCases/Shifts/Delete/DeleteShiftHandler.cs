using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Authority;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Delete;

/// <summary>Deletes a shift and every grant of it; a published one is reported as <c>shifts.cancelled.v1</c>.</summary>
public sealed class DeleteShiftHandler(IRepository<Shift> shifts, IReadRepository<Section> sections, ShiftGrants grants, ShiftAuthority authority,
    ShiftNotices notices, ShiftCaller caller) : ICommandHandler<DeleteShiftCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteShiftCommand command, CancellationToken ct)
    {
        var shift = await shifts.FirstOrDefaultAsync(new ShiftByIdSpec(command.Id), ct);
        if (shift is null) return Result.NotFound($"Shift {command.Id} was not found.");
        var allowed = await authority.RequireManageAsync(shift.UnitId, ct);
        if (!allowed.IsSuccess) return allowed;

        if (shift.Published)
        {
            var section = shift.SectionId is { } id ? await sections.FirstOrDefaultAsync(new SectionByIdSpec(id), ct) : null;
            await notices.PublishAsync(shift, ShiftsEvents.Cancelled,
                new ShiftCancelled(await notices.SnapshotAsync(shift, section, ct), caller.Me, caller.Now), ct);
        }
        await grants.ClearAsync(shift, ct);
        await shifts.DeleteAsync(shift, ct); // saves the outbox row with the delete
        if (shift.Published) await notices.NudgeAsync(shift, ct);
        return Result.Success();
    }
}
