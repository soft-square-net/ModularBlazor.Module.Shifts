using soft.square.net.WebApi.Modules.Shifts.UseCases.Authority;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Publish;

public sealed class PublishShiftHandler(ShiftChanges changes, ShiftAuthority authority, ShiftNotices notices)
    : ICommandHandler<PublishShiftCommand, Result<ShiftDto>>
{
    public async ValueTask<Result<ShiftDto>> Handle(PublishShiftCommand command, CancellationToken ct) =>
        await changes.ChangeWithSectionAsync(command.Id, async change =>
        {
            var (shift, section) = change;
            var allowed = await authority.RequireManageAsync(shift.UnitId, ct);
            if (!allowed.IsSuccess) return allowed;
            var first = !shift.Published;
            shift.Publish();
            if (first) await notices.PublishAsync(shift, ShiftsEvents.Published, new ShiftPublished(await notices.SnapshotAsync(shift, section, ct)), ct);
            return Result.Success();
        }, ct);
}
