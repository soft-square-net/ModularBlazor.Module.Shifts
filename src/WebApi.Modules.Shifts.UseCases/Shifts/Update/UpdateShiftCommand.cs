namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Update;

/// <summary>
/// Reschedule a shift and replace its rota. It stays in its section and its unit (a <paramref name="UnitId"/> may only
/// be given to a shift that has none yet); a published shift's grants follow.
/// </summary>
public sealed record UpdateShiftCommand(int Id, int SectionId, DateTimeOffset StartsAt, DateTimeOffset EndsAt, IReadOnlyList<RotaEntry> Rota,
    Guid? UnitId = null) : ICommand<Result<ShiftDto>>;
