namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Add;

/// <summary>
/// Plan a shift with its rota (exactly one Manager). Nothing is granted until it is published. Its Organization unit:
/// <paramref name="UnitId"/> (a cached unit), or, left out, the unit the section moved to; a section that has not moved
/// yet may plan shifts without a unit. A moved section refuses any other unit.
/// </summary>
public sealed record AddShiftCommand(int SectionId, DateTimeOffset StartsAt, DateTimeOffset EndsAt, IReadOnlyList<RotaEntry> Rota, Guid? UnitId = null)
    : ICommand<Result<ShiftDto>>;
