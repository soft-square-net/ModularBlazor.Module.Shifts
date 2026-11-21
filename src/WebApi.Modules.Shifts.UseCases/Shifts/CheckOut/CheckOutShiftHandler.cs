namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.CheckOut;

public sealed class CheckOutShiftHandler(ShiftChanges changes, ShiftCaller caller) : ICommandHandler<CheckOutShiftCommand, Result<ShiftDto>>
{
    public async ValueTask<Result<ShiftDto>> Handle(CheckOutShiftCommand command, CancellationToken ct) =>
        await changes.ChangeAsync(command.Id, async shift =>
        {
            var me = caller.Me;
            var now = caller.Now;
            if (!(shift.IsManager(me) && shift.IsRunning(now)) && !await caller.IsShiftAdminAsync(ct))
                return Result.Forbidden("Only the shift's manager (while it runs) or a shift admin checks a shift out.");
            shift.CheckOut(me, now);
            return Result.Success();
        }, ct);
}
