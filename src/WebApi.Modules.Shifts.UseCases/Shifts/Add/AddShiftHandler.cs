using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Authority;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Roles;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Sections;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Add;

public sealed class AddShiftHandler(IRepository<Shift> shifts, IReadRepository<Section> sections, ShiftUnits units, ShiftRota rota,
    ShiftAuthority authority, ShiftDetails details) : ICommandHandler<AddShiftCommand, Result<ShiftDto>>
{
    public async ValueTask<Result<ShiftDto>> Handle(AddShiftCommand command, CancellationToken ct)
    {
        if (await sections.GetByIdAsync(command.SectionId, ct) is null) return SectionDto.NotFound<ShiftDto>(command.SectionId);
        var unit = await units.ResolveAsync(command.SectionId, command.UnitId, ct);
        if (!unit.IsSuccess) return unit.As<Guid?, ShiftDto>();
        var allowed = await authority.RequireManageAsync(unit.Value, ct);
        if (!allowed.IsSuccess) return allowed.As<ShiftDto>();
        var assignments = await rota.ResolveAsync(unit.Value, command.Rota, command.StartsAt, ct);
        if (!assignments.IsSuccess) return assignments.As<IReadOnlyList<RotaAssignment>, ShiftDto>();

        var shift = Shift.Create(command.SectionId, command.StartsAt, command.EndsAt, assignments.Value, unit.Value);
        await shifts.AddAsync(shift, ct);
        return (await details.ToDtosAsync([shift], ct))[0];
    }
}
