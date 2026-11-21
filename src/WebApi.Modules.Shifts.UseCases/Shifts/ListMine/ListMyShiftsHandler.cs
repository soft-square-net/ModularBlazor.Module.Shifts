using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate.Specifications;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.ListMine;

public sealed class ListMyShiftsHandler(ShiftDetails details, ShiftCaller caller) : IQueryHandler<ListMyShiftsQuery, Result<IReadOnlyList<ShiftDto>>>
{
    public async ValueTask<Result<IReadOnlyList<ShiftDto>>> Handle(ListMyShiftsQuery query, CancellationToken ct)
    {
        var now = caller.Now;
        return Result.Success(await details.ListAsync(new MemberShiftsSpec(caller.Me, now, now.AddDays(7)), ct));
    }
}
