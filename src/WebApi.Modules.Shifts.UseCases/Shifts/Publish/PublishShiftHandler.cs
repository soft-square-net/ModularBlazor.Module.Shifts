using soft.square.net.WebApi.Modules.Shifts.UseCases.Authority;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Publish;

public sealed class PublishShiftHandler(ShiftChanges changes, ShiftAuthority authority) : ICommandHandler<PublishShiftCommand, Result<ShiftDto>>
{
    public async ValueTask<Result<ShiftDto>> Handle(PublishShiftCommand command, CancellationToken ct) =>
        await changes.ChangeAsync(command.Id, async shift =>
        {
            var allowed = await authority.RequireManageAsync(shift.UnitId, ct);
            if (!allowed.IsSuccess) return allowed;
            shift.Publish();
            return Result.Success();
        }, ct);
}
