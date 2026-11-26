using soft.square.net.WebApi.Modules.Shifts.UseCases.Authority;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.CheckOutMember;

public sealed class CheckOutMemberHandler(ShiftChanges changes, ShiftCaller caller, ShiftAuthority authority, ShiftNotices notices)
    : ICommandHandler<CheckOutMemberCommand, Result<ShiftDto>>
{
    public async ValueTask<Result<ShiftDto>> Handle(CheckOutMemberCommand command, CancellationToken ct) =>
        await changes.ChangeWithSectionAsync(command.Id, async change =>
        {
            var (shift, section) = change;
            var me = caller.Me;
            if (command.UserId != me && !shift.IsLead(me) && !await authority.CanManageAsync(shift.UnitId, ct))
                return Result.Forbidden("Members check themselves out; the shift's lead or a manager of its unit can check anyone out.");
            if (!shift.HasMember(command.UserId)) return Result.NotFound($"'{command.UserId}' is not on shift {shift.Id}.");

            var now = caller.Now;
            shift.CheckOutMember(command.UserId, me, now);
            await notices.PublishAsync(shift, ShiftsEvents.CheckedOut,
                new ShiftCheckedOut(shift.Id, shift.UnitId, command.UserId, now, me, false,
                    await notices.StatusOfAsync(shift, section, command.UserId, ct)), ct);
            return Result.Success();
        }, ct);
}
