using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate.Specifications;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.ListOverdue;

public sealed class ListOverdueShiftsHandler(ShiftDetails details, ShiftCaller caller) : IQueryHandler<ListOverdueShiftsQuery, Result<IReadOnlyList<ShiftDto>>>
{
    public async ValueTask<Result<IReadOnlyList<ShiftDto>>> Handle(ListOverdueShiftsQuery query, CancellationToken ct) =>
        Result.Success(await details.ListAsync(new OverdueShiftsSpec(caller.Now), ct));
}
