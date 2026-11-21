using soft.square.net.WebApi.SharedKernel;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Extensions;

/// <summary>
/// The exceptions that still reach the endpoints, as problem details: a broken domain rule (the aggregate throws
/// <see cref="DomainException"/>) is 409; a caller who is not signed in is 403. Expected outcomes come back as an
/// Ardalis Result instead (see <see cref="ResultExtensions"/>). On the module's route group, so it is module code and
/// unloads with the module.
/// </summary>
public sealed class ShiftsProblemsFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try { return await next(context); }
        catch (ShiftAccessDeniedException e) { return Results.Problem(e.Message, statusCode: StatusCodes.Status403Forbidden); }
        catch (DomainException e) { return Results.Problem(e.Message, statusCode: StatusCodes.Status409Conflict); }
    }
}
