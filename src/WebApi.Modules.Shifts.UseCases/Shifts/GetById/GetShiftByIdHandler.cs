namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.GetById;

public sealed class GetShiftByIdHandler(ShiftDetails details) : IQueryHandler<GetShiftByIdQuery, Result<ShiftDto>>
{
    public async ValueTask<Result<ShiftDto>> Handle(GetShiftByIdQuery query, CancellationToken ct) => await details.GetAsync(query.Id, ct);
}
