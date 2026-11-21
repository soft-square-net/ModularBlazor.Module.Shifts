using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate.Specifications;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.List;

public sealed class ListShiftsHandler(ShiftDetails details, ShiftCaller caller) : IQueryHandler<ListShiftsQuery, Result<IReadOnlyList<ShiftDto>>>
{
    public async ValueTask<Result<IReadOnlyList<ShiftDto>>> Handle(ListShiftsQuery query, CancellationToken ct)
    {
        var now = caller.Now;
        var spec = new ShiftsInWindowSpec(query.From ?? now.AddDays(-1), query.To ?? now.AddDays(7), query.SectionId);
        return Result.Success(await details.ListAsync(spec, ct));
    }
}
