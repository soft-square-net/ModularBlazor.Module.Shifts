using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using soft.square.net.ModularBlazor.Abstractions;
using soft.square.net.ModularBlazor.Abstractions.MudTail;
using soft.square.net.WebApi.Modules.Shifts.Client;

namespace soft.square.net.Module.Shifts;

/// <summary>
/// What every Shifts Management page shares: the Shifts API client on the host's "WebApi" HttpClient (the gateway,
/// with the access token), the signed-in user's id, one-call-at-a-time state and readable error messages.
/// </summary>
public abstract class ShiftsPageBase : ComponentBase
{
    [Inject] protected IHttpClientFactory HttpClients { get; set; } = default!;
    [Inject] protected MudTailRuntime MudTail { get; set; } = default!;
    [Inject] protected NavigationManager Nav { get; set; } = default!;
    [Inject] protected AuthenticationStateProvider Auth { get; set; } = default!;

    /// <summary>Shifts routes (/api/shifts/...) reach the pool that runs the module through the gateway.</summary>
    protected ShiftsClient Api => new(HttpClients.CreateClient(HttpClientNames.WebApi));

    protected bool Busy { get; private set; }
    protected string? Error { get; set; }

    /// <summary>The signed-in user's id (the token's <c>sub</c>): what the rota and the API's check-out rules use.</summary>
    protected string? Me { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        var user = (await Auth.GetAuthenticationStateAsync()).User;
        Me = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>Runs one API call: busy flag, error message and toast, and <paramref name="then"/> when it succeeded.</summary>
    protected async Task<bool> RunAsync(Func<Task> call, Func<Task>? then = null, bool toast = true)
    {
        Busy = true;
        Error = null;
        StateHasChanged();
        try
        {
            await call();
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect(); // the session ended: sign in again and come back
            return false;
        }
        catch (Exception ex)
        {
            Error = Describe(ex);
            if (toast) await MudTail.ToastAsync(Error, "error");
            return false;
        }
        finally
        {
            Busy = false;
            StateHasChanged();
        }

        if (then is not null) await then();
        StateHasChanged();
        return true;
    }

    protected static string Describe(Exception ex) => ex switch
    {
        ShiftsApiException { StatusCode: 400 } api when Mentions(api, "tenant") =>
            "400: no tenant. Shifts belong to a tenant: sign in to one (your organisation) to manage its shifts.",
        ShiftsApiException { StatusCode: 401 } => "401: the gateway did not accept the access token. Sign in again.",
        ShiftsApiException { StatusCode: 403 } api =>
            $"403: {ProblemDetail(api) ?? "your account lacks the shifts permission for this (shifts.view or shifts.manage in this tenant)."}",
        ShiftsApiException { StatusCode: 404 } api => $"404: {ProblemDetail(api) ?? "not found, or not visible to you."}",
        ShiftsApiException { StatusCode: 409 } api => $"409: {ProblemDetail(api) ?? "the shift does not allow this now. Reload and try again."}",
        ShiftsApiException api => $"{api.StatusCode}: {ProblemDetail(api) ?? FirstLine(api.Response) ?? FirstLine(api.Message)}",
        HttpRequestException http => $"The gateway is not reachable ({http.Message}).",
        _ => ex.Message,
    };

    /// <summary>The "detail" (else "title") of a problem-details response body, if it is one.</summary>
    private static string? ProblemDetail(ShiftsApiException api)
    {
        if (string.IsNullOrWhiteSpace(api.Response) || !api.Response.TrimStart().StartsWith('{')) return null;
        try
        {
            using var json = System.Text.Json.JsonDocument.Parse(api.Response);
            foreach (var name in new[] { "detail", "title" })
                if (json.RootElement.TryGetProperty(name, out var value) && value.GetString() is { Length: > 0 } text)
                    return text;
        }
        catch (System.Text.Json.JsonException) { }
        return null;
    }

    private static bool Mentions(ShiftsApiException api, string word) =>
        (api.Response ?? "").Contains(word, StringComparison.OrdinalIgnoreCase);

    private static string? FirstLine(string? text)
        => string.IsNullOrWhiteSpace(text) ? null : text.Split('\n', 2)[0].Trim();

    protected static string When(DateTimeOffset? at) => at?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "";

    /// <summary>Where a shift stands, in words.</summary>
    protected static string Status(ShiftDto shift) => shift switch
    {
        { CheckedOutAt: not null } => "Checked out",
        { Running: true, Published: true } => "Running",
        { Running: true } => "Running, not published",
        { Published: true } when shift.EndsAt <= DateTimeOffset.UtcNow => "Overdue",
        { Published: true } => "Published",
        _ when shift.EndsAt <= DateTimeOffset.UtcNow => "Ended, not published",
        _ => "Planned"
    };

    protected static readonly string[] Statuses =
        ["Planned", "Published", "Running", "Running, not published", "Overdue", "Ended, not published", "Checked out"];

    /// <summary>A short label for a user id: the signed-in user is "you".</summary>
    protected string Who(string? userId) => userId is null ? "" : userId == Me ? $"you ({Short(userId)})" : userId;

    private static string Short(string id) => id.Length > 8 ? id[..8] + "…" : id;
}
