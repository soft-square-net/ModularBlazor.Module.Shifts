using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate.Specifications;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Delete;

public sealed class DeleteShiftHandler(IRepository<Shift> shifts, ShiftGrants grants) : ICommandHandler<DeleteShiftCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteShiftCommand command, CancellationToken ct)
    {
        var shift = await shifts.FirstOrDefaultAsync(new ShiftByIdSpec(command.Id), ct);
        if (shift is null) return Result.NotFound($"Shift {command.Id} was not found.");

        await grants.ClearAsync(shift, ct);
        await shifts.DeleteAsync(shift, ct);
        return Result.Success();
    }
}
