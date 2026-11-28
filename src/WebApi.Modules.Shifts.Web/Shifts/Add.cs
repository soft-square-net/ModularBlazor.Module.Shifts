using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Add;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Shifts;

/// <summary>POST /shifts: plan a shift with its rota.</summary>
public sealed class Add : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapPost("/shifts", HandleAsync)
            .RequirePermission(ShiftPermissions.Manage)
            .WithName("Shifts_AddShift")
            .WithSummary("Plans a shift with its rota: role keys that hold for the unit (GET /roles/effective), at most one person in a lead role (exactly one to publish), and for a shift of a unit only members of the unit or a unit above it. Needs shifts.manage on the unit (tenant-wide for a shift with no unit). Its unit: unitId (a cached Organization unit), or the unit its section moved to; a section that has not moved yet may plan shifts without a unit. sectionId is optional: with a unit only, the shift takes the section that moved to that unit, if any. Nothing is granted until it is published.");

    private static async Task<Results<Created<ShiftDto>, ProblemHttpResult>> HandleAsync(ShiftRequest r, ISender sender, CancellationToken ct) =>
        (await sender.Send(new AddShiftCommand(r.SectionId, r.StartsAt, r.EndsAt, r.Rota, r.UnitId), ct))
            .ToCreatedResult(shift => $"shifts/{shift.Id}");
}
