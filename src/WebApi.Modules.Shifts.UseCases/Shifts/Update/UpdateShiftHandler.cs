namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Update;

public sealed class UpdateShiftHandler(ShiftChanges changes, ShiftUnits units) : ICommandHandler<UpdateShiftCommand, Result<ShiftDto>>
{
    public async ValueTask<Result<ShiftDto>> Handle(UpdateShiftCommand command, CancellationToken ct) =>
        await changes.ChangeAsync(command.Id, async shift =>
        {
            if (shift.SectionId != command.SectionId)
                return Result.Conflict("A shift cannot move to another section; delete it and plan a new one.");
            if (command.UnitId is { } unitId && shift.UnitId is { } current && current != unitId)
                return Result.Conflict("A shift cannot move to another unit; delete it and plan a new one.");
            if (command.UnitId is not null && shift.UnitId is null)
            {
                var unit = await units.ResolveAsync(shift.SectionId, command.UnitId, ct);
                if (!unit.IsSuccess) return Result.Invalid([.. unit.ValidationErrors]); // ShiftUnits only refuses as Invalid
                if (unit.Value is { } resolved) shift.MoveToUnit(resolved);
            }
            shift.Reschedule(command.StartsAt, command.EndsAt);
            shift.SetRota(command.Rota.Select(r => (r.UserId, r.Role)));
            return Result.Success();
        }, ct);
}
