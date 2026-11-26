using Carter;
using Attendance = soft.square.net.WebApi.Modules.Shifts.Web.Attendance;
using Roles = soft.square.net.WebApi.Modules.Shifts.Web.Roles;
using Sections = soft.square.net.WebApi.Modules.Shifts.Web.Sections;
using Shifts = soft.square.net.WebApi.Modules.Shifts.Web.Shifts;
using Templates = soft.square.net.WebApi.Modules.Shifts.Web.Templates;
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
        Attendance.GetUnitAttendance.Map(shifts);
        Attendance.SetUnitAttendance.Map(shifts);

        Roles.List.Map(shifts);
        Roles.ListEffective.Map(shifts);
        Roles.Add.Map(shifts);
        Roles.Update.Map(shifts);
        Roles.Delete.Map(shifts);

        Templates.List.Map(shifts);
        Templates.GetById.Map(shifts);
        Templates.Add.Map(shifts);
        Templates.Update.Map(shifts);
        Templates.Delete.Map(shifts);
        Templates.Clone.Map(shifts);

        Shifts.ListMine.Map(shifts);
        Shifts.ListOverdue.Map(shifts);
        Shifts.ListEnded.Map(shifts);
        Shifts.List.Map(shifts);
        Shifts.GetById.Map(shifts);
        Shifts.GetAutomation.Map(shifts);
        Shifts.Add.Map(shifts);
        Shifts.Update.Map(shifts);
        Shifts.Publish.Map(shifts);
        Shifts.CheckIn.Map(shifts);
        Shifts.CheckIn.MapMember(shifts);
        Shifts.CheckOut.Map(shifts);
        Shifts.CheckOutMember.Map(shifts);
        Shifts.Handover.Map(shifts);
        Shifts.Delete.Map(shifts);
    }
}
