using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate.Specifications;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.ListEnded;

/// <summary>
/// A shift that ended: <paramref name="EndedAt"/> is when it actually ended (the whole-shift check-out, or the clock
/// tick); <paramref name="EndedBy"/> the user, or <c>shifts:clock</c>. <paramref name="CheckedOutAt"/> is when the
/// members' grants ended (for the clock: the end plus the grace).
/// </summary>
public sealed record EndedShiftDto(int Id, Guid? UnitId, int? SectionId, DateTimeOffset StartsAt, DateTimeOffset EndsAt, DateTimeOffset EndedAt,
    string EndedBy, DateTimeOffset? CheckedOutAt);

/// <summary>
/// Shifts that ended at or after <paramref name="Since"/>, ordered by the time they ended (at most <paramref name="Take"/>,
/// 1 to 500, default 200). For the workflow's handover: it polls with a lookback, so a tick it missed is never lost.
/// </summary>
public sealed record ListEndedShiftsQuery(DateTimeOffset Since, int? Take = null) : IQuery<Result<IReadOnlyList<EndedShiftDto>>>;

public sealed class ListEndedShiftsHandler(IReadRepository<Shift> shifts) : IQueryHandler<ListEndedShiftsQuery, Result<IReadOnlyList<EndedShiftDto>>>
{
    public const int DefaultTake = 200;
    public const int MaxTake = 500;

    public async ValueTask<Result<IReadOnlyList<EndedShiftDto>>> Handle(ListEndedShiftsQuery query, CancellationToken ct)
    {
        var take = Math.Clamp(query.Take ?? DefaultTake, 1, MaxTake);
        var list = await shifts.ListAsync(new EndedShiftsSpec(query.Since.ToUniversalTime(), take), ct);
        return list.Select(s => new EndedShiftDto(s.Id, s.UnitId, s.SectionId, s.StartsAt, s.EndsAt, s.EndedAt!.Value, s.EndedBy ?? "", s.CheckedOutAt))
            .ToArray();
    }
}
