using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Shifts.Handover;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Shifts;

/// <summary>The body of <see cref="Handover"/>. Its name is the OpenAPI schema name: keep it stable.</summary>
public sealed record HandoverRequest(string Note);

/// <summary>PUT /shifts/{id}/handover: the shift's lead writes the handover note (published as shifts.handover.v1).</summary>
public sealed class Handover : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapPut("/shifts/{id:int}/handover", HandleAsync)
            .WithName("Shifts_WriteHandover")
            .WithSummary("The shift's lead writes (or rewrites) the handover note, once the shift has started; published as shifts.handover.v1.");

    private static async Task<Results<Ok<ShiftDto>, ProblemHttpResult>> HandleAsync(int id, HandoverRequest request, ISender sender, CancellationToken ct) =>
        (await sender.Send(new WriteHandoverCommand(id, request.Note), ct)).ToOkResult();
}
