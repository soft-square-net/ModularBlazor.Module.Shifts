using soft.square.net.WebApi.Modules.Shifts.UseCases.Attendance;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Authority;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.CheckIn;

public sealed class CheckInHandler(ShiftChanges changes, ShiftCaller caller, ShiftAuthority authority, ShiftThresholds thresholds, ShiftNotices notices)
    : ICommandHandler<CheckInCommand, Result<ShiftDto>>
{
    public async ValueTask<Result<ShiftDto>> Handle(CheckInCommand command, CancellationToken ct) =>
        await changes.ChangeWithSectionAsync(command.Id, async change =>
        {
            var (shift, section) = change;
            var me = caller.Me;
            var userId = string.IsNullOrWhiteSpace(command.UserId) ? me : command.UserId.Trim();
            if (userId != me && !shift.IsLead(me) && !await authority.CanManageAsync(shift.UnitId, ct))
                return Result.Forbidden("Members check themselves in; the shift's lead or a manager of its unit can check anyone in.");
            if (!shift.HasMember(userId)) return Result.NotFound($"'{userId}' is not on shift {shift.Id}.");

            var now = caller.Now;
            shift.CheckIn(userId, me, now, await thresholds.ForAsync(shift, section, ct));
            await notices.PublishAsync(shift, ShiftsEvents.CheckedIn,
                new ShiftCheckedIn(shift.Id, shift.UnitId, userId, [.. shift.Members.Where(m => m.UserId == userId).Select(m => m.RoleKey)], now, me,
                    await notices.StatusOfAsync(shift, section, userId, ct)), ct);
            return Result.Success();
        }, ct);
}
