namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.CheckOutMember;

/// <summary>
/// A checked-in member checks out and their grants end now (the live hub drops their topics at once): themselves, the
/// shift's lead, or a manager of its unit.
/// </summary>
public sealed record CheckOutMemberCommand(int Id, string UserId) : ICommand<Result<ShiftDto>>;
