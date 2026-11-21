namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Update;

public sealed class UpdateShiftHandler(ShiftChanges changes) : ICommandHandler<UpdateShiftCommand, Result<ShiftDto>>
{
    public async ValueTask<Result<ShiftDto>> Handle(UpdateShiftCommand command, CancellationToken ct) =>
        await changes.ChangeAsync(command.Id, shift =>
        {
            if (shift.SectionId != command.SectionId)
                return Task.FromResult(Result.Conflict("A shift cannot move to another section; delete it and plan a new one."));
            shift.Reschedule(command.StartsAt, command.EndsAt);
            shift.SetRota(command.Rota.Select(r => (r.UserId, r.Role)));
            return Task.FromResult(Result.Success());
        }, ct);
}
