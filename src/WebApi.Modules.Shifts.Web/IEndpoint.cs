namespace soft.square.net.WebApi.Modules.Shifts.Web;

/// <summary>
/// One HTTP endpoint (REPR: its request type, the endpoint, its response). <see cref="ShiftsRoutes"/> maps it.
/// The endpoint sets its route, permission, name (the OpenAPI operation id, <c>Shifts_&lt;Operation&gt;</c>, unique
/// across the host; generated clients take their method names from it) and summary, and sends one command or query.
/// </summary>
public interface IEndpoint
{
    static abstract RouteHandlerBuilder Map(IEndpointRouteBuilder app);
}
