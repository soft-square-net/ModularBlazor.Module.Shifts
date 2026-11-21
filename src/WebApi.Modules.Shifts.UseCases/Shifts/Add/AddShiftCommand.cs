namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Add;

/// <summary>Plan a shift with its rota (exactly one Manager). Nothing is granted until it is published.</summary>
public sealed record AddShiftCommand(int SectionId, DateTimeOffset StartsAt, DateTimeOffset EndsAt, IReadOnlyList<RotaEntry> Rota)
    : ICommand<Result<ShiftDto>>;
