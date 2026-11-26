using soft.square.net.WebApi.Modules.Shifts.UseCases.Authority;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.CheckOut;

public sealed class CheckOutShiftHandler(ShiftChanges changes, ShiftCaller caller, ShiftAuthority authority)
    : ICommandHandler<CheckOutShiftCommand, Result<ShiftDto>>
{
    public async ValueTask<Result<ShiftDto>> Handle(CheckOutShiftCommand command, CancellationToken ct) =>
        await changes.ChangeAsync(command.Id, async shift =>
        {
            var me = caller.Me;
            var now = caller.Now;
            if (!(shift.IsLead(me) && shift.IsRunning(now)) && !await authority.CanManageAsync(shift.UnitId, ct))
                return Result.Forbidden("Only the shift's lead (while it runs) or a manager of its unit checks a shift out.");
            shift.CheckOut(me, now);
            return Result.Success();
        }, ct);
}
