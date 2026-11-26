using Carter;
using Sections = soft.square.net.WebApi.Modules.Shifts.Web.Sections;
using Shifts = soft.square.net.WebApi.Modules.Shifts.Web.Shifts;
using Units = soft.square.net.WebApi.Modules.Shifts.Web.Units;

namespace soft.square.net.WebApi.Modules.Shifts.Web;

/// <summary>
/// The module's route table, and its only Carter module (the runtime maps the Carter modules of the entry assembly,
/// under <c>/api/shifts</c>). Every endpoint is listed here once. A new endpoint: add its file under the resource
/// folder, then one line here.
/// </summary>
public sealed class ShiftsRoutes : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var shifts = app.MapGroup("").AddEndpointFilter<ShiftsProblemsFilter>();

        Sections.List.Map(shifts);
        Sections.ListServing.Map(shifts);
        Sections.Add.Map(shifts);
        Sections.Update.Map(shifts);
        Sections.GetSettings.Map(shifts);
        Sections.Export.Map(shifts);
        Sections.MoveToOrganization.Map(shifts);

        Units.List.Map(shifts);
        Units.Resync.Map(shifts);

        Shifts.ListMine.Map(shifts);
        Shifts.ListOverdue.Map(shifts);
        Shifts.List.Map(shifts);
        Shifts.GetById.Map(shifts);
        Shifts.GetAutomation.Map(shifts);
        Shifts.Add.Map(shifts);
        Shifts.Update.Map(shifts);
        Shifts.Publish.Map(shifts);
        Shifts.CheckOut.Map(shifts);
        Shifts.CheckOutMember.Map(shifts);
        Shifts.Delete.Map(shifts);
    }
}
