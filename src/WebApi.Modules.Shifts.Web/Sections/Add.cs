using soft.square.net.WebApi.Modules.Shifts.UseCases.Sections;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Sections.Add;

namespace soft.square.net.WebApi.Modules.Shifts.Web.Sections;

/// <summary>POST /sections: add a section.</summary>
public sealed class Add : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapPost("/sections", HandleAsync)
            .RequirePermission(ShiftPermissions.Manage)
            .WithName("Shifts_AddSection");

    private static async Task<Results<Created<SectionDto>, ProblemHttpResult>> HandleAsync(SectionRequest r, ISender sender, CancellationToken ct) =>
        (await sender.Send(new AddSectionCommand(r.Name, r.Categories, r.AutoAssignDelayMinutes, r.CheckoutGraceMinutes), ct))
            .ToCreatedResult(section => $"sections/{section.Id}");
}
