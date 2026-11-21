namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Delete;

/// <summary>Deletes a shift and its grants.</summary>
public sealed record DeleteShiftCommand(int Id) : ICommand<Result>;
