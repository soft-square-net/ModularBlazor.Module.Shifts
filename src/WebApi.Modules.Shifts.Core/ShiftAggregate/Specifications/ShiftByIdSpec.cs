namespace soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate.Specifications;

/// <summary>The shift with this id and its rota.</summary>
public sealed class ShiftByIdSpec : SingleResultSpecification<Shift>
{
    public ShiftByIdSpec(int id) => Query.Where(s => s.Id == id).Include(s => s.Members);
}
