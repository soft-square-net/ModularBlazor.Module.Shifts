using soft.square.net.WebApi.Modules.Shifts.UseCases.Authority;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.CheckOutMember;

public sealed class CheckOutMemberHandler(ShiftChanges changes, ShiftCaller caller, ShiftAuthority authority)
    : ICommandHandler<CheckOutMemberCommand, Result<ShiftDto>>
{
    public async ValueTask<Result<ShiftDto>> Handle(CheckOutMemberCommand command, CancellationToken ct) =>
        await changes.ChangeAsync(command.Id, async shift =>
        {
            var me = caller.Me;
            if (command.UserId != me && !shift.IsLead(me) && !await authority.CanManageAsync(shift.UnitId, ct))
                return Result.Forbidden("Members check themselves out; the shift's lead or a manager of its unit can check anyone out.");
            shift.CheckOutMember(command.UserId, caller.Now);
            return Result.Success();
        }, ct);
}
