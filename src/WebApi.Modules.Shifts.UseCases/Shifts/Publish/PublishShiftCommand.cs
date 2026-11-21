namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Publish;

/// <summary>Writes the rota's grants; from now on the members' rights follow the shift's clock.</summary>
public sealed record PublishShiftCommand(int Id) : ICommand<Result<ShiftDto>>;
