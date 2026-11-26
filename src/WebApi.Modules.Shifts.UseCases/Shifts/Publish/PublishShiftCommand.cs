namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Publish;

/// <summary>
/// Publishes the shift (<c>shifts.published.v1</c>): its members may now check in, and each one's grants start when
/// they do. Publishing gives nobody rights by itself.
/// </summary>
public sealed record PublishShiftCommand(int Id) : ICommand<Result<ShiftDto>>;
