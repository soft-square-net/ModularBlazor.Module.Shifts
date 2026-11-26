using soft.square.net.WebApi.Modules.Shifts.UseCases.Authority;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.CheckOut;

public sealed class CheckOutShiftHandler(ShiftChanges changes, ShiftCaller caller, ShiftAuthority authority, ShiftNotices notices)
    : ICommandHandler<CheckOutShiftCommand, Result<ShiftDto>>
{
    public async ValueTask<Result<ShiftDto>> Handle(CheckOutShiftCommand command, CancellationToken ct) =>
        await changes.ChangeWithSectionAsync(command.Id, async change =>
        {
            var (shift, section) = change;
            var me = caller.Me;
            if (!shift.IsLead(me) && !await authority.CanManageAsync(shift.UnitId, ct))
                return Result.Forbidden("Only the shift's lead or a manager of its unit checks a shift out.");
            var checkedOut = shift.CheckOut(me, caller.Now);
            await notices.PublishEndAsync(shift, section, checkedOut, me, automatic: false, ct);
            return Result.Success();
        }, ct);
}
