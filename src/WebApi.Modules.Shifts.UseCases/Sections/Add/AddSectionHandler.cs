using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Sections.Add;

public sealed class AddSectionHandler(IRepository<Section> sections, IReadRepository<SectionUnitMap> moved)
    : ICommandHandler<AddSectionCommand, Result<SectionDto>>
{
    public async ValueTask<Result<SectionDto>> Handle(AddSectionCommand command, CancellationToken ct)
    {
        if (await moved.AnyAsync(ct)) return Result<SectionDto>.Conflict(SectionDto.MovedToOrganization);
        var section = Section.Create(command.Name, command.Categories, command.AutoAssignDelayMinutes, command.CheckoutGraceMinutes,
            command.Kind ?? SectionKind.Category);
        await sections.AddAsync(section, ct);
        return SectionDto.From(section);
    }
}
