namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Add;

/// <summary>
/// Plan a shift with its rota (exactly one Manager). Nothing is granted until it is published. The shift belongs to an
/// Organization unit the module has cached: <paramref name="UnitId"/>, or, when it is left out, the unit the section
/// moved to.
/// </summary>
public sealed record AddShiftCommand(int SectionId, DateTimeOffset StartsAt, DateTimeOffset EndsAt, IReadOnlyList<RotaEntry> Rota, Guid? UnitId = null)
    : ICommand<Result<ShiftDto>>;
