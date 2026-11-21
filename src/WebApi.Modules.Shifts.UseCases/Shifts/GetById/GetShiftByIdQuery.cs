namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.GetById;

/// <summary>One shift with its rota.</summary>
public sealed record GetShiftByIdQuery(int Id) : IQuery<Result<ShiftDto>>;
