namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.CheckIn;

/// <summary>
/// A member checks in, and their grants start now: themselves (<paramref name="UserId"/> null or their own id), or the
/// shift's lead or a manager of its unit for them. Check-in opens the early threshold before the start and closes at
/// the end.
/// </summary>
public sealed record CheckInCommand(int Id, string? UserId = null) : ICommand<Result<ShiftDto>>;
