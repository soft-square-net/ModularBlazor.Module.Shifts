using soft.square.net.WebApi.Modules.Shifts.UseCases.Authority;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Roles;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Update;

public sealed class UpdateShiftHandler(ShiftChanges changes, ShiftUnits units, ShiftRota rota, ShiftAuthority authority, ShiftNotices notices)
    : ICommandHandler<UpdateShiftCommand, Result<ShiftDto>>
{
    public async ValueTask<Result<ShiftDto>> Handle(UpdateShiftCommand command, CancellationToken ct) =>
        await changes.ChangeWithSectionAsync(command.Id, async change =>
        {
            var (shift, section) = change;
            var allowed = await authority.RequireManageAsync(shift.UnitId, ct);
            if (!allowed.IsSuccess) return allowed;
            if (shift.SectionId != command.SectionId)
                return Result.Conflict("A shift cannot move to another section; delete it and plan a new one.");
            if (command.UnitId is { } unitId && shift.UnitId is { } current && current != unitId)
                return Result.Conflict("A shift cannot move to another unit; delete it and plan a new one.");

            var unit = shift.UnitId;
            if (command.UnitId is not null && shift.UnitId is null)
            {
                var resolved = await units.ResolveAsync(command.SectionId, command.UnitId, ct);
                if (!resolved.IsSuccess) return Result.Invalid([.. resolved.ValidationErrors]); // ShiftUnits only refuses as Invalid
                unit = resolved.Value;
                if (unit is not null)
                {
                    allowed = await authority.RequireManageAsync(unit, ct);
                    if (!allowed.IsSuccess) return allowed;
                }
            }
            var assignments = await rota.ResolveAsync(unit, command.Rota, command.StartsAt, ct);
            if (!assignments.IsSuccess) return Failed(assignments);

            var (before, startsAt, endsAt, unitBefore) = (shift.Rota, shift.StartsAt, shift.EndsAt, shift.UnitId);
            if (unit is { } moved && shift.UnitId is null) shift.MoveToUnit(moved);
            shift.Reschedule(command.StartsAt, command.EndsAt);
            shift.SetRota(assignments.Value);

            // A published shift reports its changes: the rota's, then the shift as it is now.
            if (!shift.Published) return Result.Success();
            var rotaChanged = await notices.PublishRotaChangesAsync(shift, before, ct);
            if (rotaChanged || shift.StartsAt != startsAt || shift.EndsAt != endsAt || shift.UnitId != unitBefore)
                await notices.PublishAsync(shift, ShiftsEvents.Updated, new ShiftUpdated(await notices.SnapshotAsync(shift, section, ct)), ct);
            return Result.Success();
        }, ct);

    private static Result Failed<T>(Result<T> failure) => failure.Status switch
    {
        ResultStatus.Invalid => Result.Invalid([.. failure.ValidationErrors]),
        ResultStatus.Unavailable => Result.Unavailable([.. failure.Errors]),
        _ => Result.Error(new ErrorList([.. failure.Errors]))
    };
}
