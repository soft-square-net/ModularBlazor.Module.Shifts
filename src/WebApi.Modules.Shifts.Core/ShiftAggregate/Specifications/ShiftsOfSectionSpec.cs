namespace soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate.Specifications;

/// <summary>Every shift of a section, with its rota (for the move to Organization).</summary>
public sealed class ShiftsOfSectionSpec : Specification<Shift>
{
    public ShiftsOfSectionSpec(int sectionId) => Query.Where(s => s.SectionId == sectionId).Include(s => s.Members);
}
