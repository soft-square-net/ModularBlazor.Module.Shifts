using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Sections;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Add;

public sealed class AddShiftHandler(IRepository<Shift> shifts, IReadRepository<Section> sections, ShiftDetails details)
    : ICommandHandler<AddShiftCommand, Result<ShiftDto>>
{
    public async ValueTask<Result<ShiftDto>> Handle(AddShiftCommand command, CancellationToken ct)
    {
        if (await sections.GetByIdAsync(command.SectionId, ct) is null) return SectionDto.NotFound<ShiftDto>(command.SectionId);

        var shift = Shift.Create(command.SectionId, command.StartsAt, command.EndsAt, command.Rota.Select(r => (r.UserId, r.Role)));
        await shifts.AddAsync(shift, ct);
        return (await details.ToDtosAsync([shift], ct))[0];
    }
}
