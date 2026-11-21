namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Update;

/// <summary>Reschedule a shift and replace its rota. It stays in its section; a published shift's grants follow.</summary>
public sealed record UpdateShiftCommand(int Id, int SectionId, DateTimeOffset StartsAt, DateTimeOffset EndsAt, IReadOnlyList<RotaEntry> Rota)
    : ICommand<Result<ShiftDto>>;
