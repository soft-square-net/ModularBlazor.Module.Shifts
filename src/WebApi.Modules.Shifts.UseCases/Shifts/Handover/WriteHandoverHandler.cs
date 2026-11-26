namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Handover;

public sealed class WriteHandoverHandler(ShiftChanges changes, ShiftCaller caller, ShiftNotices notices)
    : ICommandHandler<WriteHandoverCommand, Result<ShiftDto>>
{
    public async ValueTask<Result<ShiftDto>> Handle(WriteHandoverCommand command, CancellationToken ct) =>
        await changes.ChangeAsync(command.Id, async shift =>
        {
            var me = caller.Me;
            if (!shift.IsLead(me)) return Result.Forbidden("Only the shift's lead writes the handover note.");
            shift.WriteHandover(me, command.Note, caller.Now);
            await notices.PublishAsync(shift, ShiftsEvents.Handover,
                new ShiftHandover(shift.Id, shift.UnitId, shift.HandoverNote!, me, shift.HandoverAt!.Value), ct);
            return Result.Success();
        }, ct);
}
