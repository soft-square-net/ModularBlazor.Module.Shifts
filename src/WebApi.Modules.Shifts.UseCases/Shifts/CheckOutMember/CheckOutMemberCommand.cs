namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.CheckOutMember;

/// <summary>One member leaves early and their grants end now: themselves, the shift's manager, or a shift admin.</summary>
public sealed record CheckOutMemberCommand(int Id, string UserId) : ICommand<Result<ShiftDto>>;
