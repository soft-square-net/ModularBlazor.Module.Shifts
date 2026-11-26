using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate.Specifications;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Authority;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Sections.Update;

public sealed class UpdateSectionHandler(IRepository<Section> sections, IReadRepository<Shift> shifts, IReadRepository<SectionUnitMap> moved,
    ShiftGrants grants, ShiftCaller caller, ShiftAuthority authority) : ICommandHandler<UpdateSectionCommand, Result<SectionDto>>
{
    public async ValueTask<Result<SectionDto>> Handle(UpdateSectionCommand command, CancellationToken ct)
    {
        var allowed = await authority.RequireTenantManageAsync(ct);
        if (!allowed.IsSuccess) return allowed.As<SectionDto>();
        if (await moved.AnyAsync(ct)) return Result<SectionDto>.Conflict(SectionDto.MovedToOrganization);
        var section = await sections.FirstOrDefaultAsync(new SectionByIdSpec(command.Id), ct);
        if (section is null) return SectionDto.NotFound<SectionDto>(command.Id);

        section.Update(command.Name, command.Categories, command.AutoAssignDelayMinutes, command.CheckoutGraceMinutes, command.Kind ?? section.Kind);
        await sections.UpdateAsync(section, ct);
        foreach (var shift in await shifts.ListAsync(new ShiftsInWindowSpec(from: caller.Now, sectionId: section.Id), ct))
            await grants.WriteAsync(shift, section, ct);
        return SectionDto.From(section);
    }
}
