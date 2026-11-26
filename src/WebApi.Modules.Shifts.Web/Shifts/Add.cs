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
            .WithSummary("Plans a shift with its rota (exactly one Manager) in a cached Organization unit: unitId, or the unit its section moved to. Nothing is granted until it is published.");

    private static async Task<Results<Created<ShiftDto>, ProblemHttpResult>> HandleAsync(ShiftRequest r, ISender sender, CancellationToken ct) =>
        (await sender.Send(new AddShiftCommand(r.SectionId, r.StartsAt, r.EndsAt, r.Rota, r.UnitId), ct))
            .ToCreatedResult(shift => $"shifts/{shift.Id}");
}
