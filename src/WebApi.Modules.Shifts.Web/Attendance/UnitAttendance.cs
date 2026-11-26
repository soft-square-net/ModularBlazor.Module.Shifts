using soft.square.net.WebApi.Modules.Shifts.UseCases.Attendance;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Attendance;

/// <summary>The body of <see cref="SetUnitAttendance"/>, in minutes (null: inherited). Its name is the OpenAPI schema name: keep it stable.</summary>
public sealed record UnitAttendanceRequest(int? EarlyCheckInMinutes, int? LateMinutes, int? AbsentMinutes, int? OvertimeMinutes, int? CheckoutGraceMinutes);

/// <summary>GET /units/{id}/attendance: a unit's own attendance overrides, and the thresholds that hold for it.</summary>
public sealed class GetUnitAttendance : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet("/units/{id:guid}/attendance", HandleAsync)
            .RequirePermission(ShiftPermissions.View, ShiftPermissions.Manage)
            .WithName("Shifts_GetUnitAttendance")
            .WithSummary("A unit's own attendance overrides (null: inherited) and the effective thresholds (its ancestors', else the module's settings).");

    private static async Task<Results<Ok<UnitAttendanceDto>, ProblemHttpResult>> HandleAsync(Guid id, ISender sender, CancellationToken ct) =>
        (await sender.Send(new GetUnitAttendanceQuery(id), ct)).ToOkResult();
}

/// <summary>PUT /units/{id}/attendance: set a unit's attendance overrides (shifts.manage on the unit or above).</summary>
public sealed class SetUnitAttendance : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapPut("/units/{id:guid}/attendance", HandleAsync)
            .RequirePermission(ShiftPermissions.Manage)
            .WithName("Shifts_SetUnitAttendance")
            .WithSummary("Sets a unit's attendance overrides in minutes (early check-in, late, absent, overtime, check-out grace); null inherits.");

    private static async Task<Results<Ok<UnitAttendanceDto>, ProblemHttpResult>> HandleAsync(Guid id, UnitAttendanceRequest r, ISender sender,
        CancellationToken ct) =>
        (await sender.Send(new SetUnitAttendanceCommand(id, r.EarlyCheckInMinutes, r.LateMinutes, r.AbsentMinutes, r.OvertimeMinutes,
            r.CheckoutGraceMinutes), ct)).ToOkResult();
}
