namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Publish;

public sealed class PublishShiftHandler(ShiftChanges changes) : ICommandHandler<PublishShiftCommand, Result<ShiftDto>>
{
    public async ValueTask<Result<ShiftDto>> Handle(PublishShiftCommand command, CancellationToken ct) =>
        await changes.ChangeAsync(command.Id, shift => shift.Publish(), ct);
}
