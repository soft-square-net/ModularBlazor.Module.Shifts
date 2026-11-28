using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using soft.square.net.Module.Shifts.Components;
using soft.square.net.Module.Shifts.Model;
using soft.square.net.ModularBlazor.Abstractions;
using soft.square.net.ModularBlazor.Abstractions.Live;
using soft.square.net.ModularBlazor.Abstractions.MudTail;
using soft.square.net.WebApi.Modules.Shifts.Client;

namespace soft.square.net.Module.Shifts;

/// <summary>
/// What every Shifts Management page shares: the Shifts API client on the host's "WebApi" HttpClient (the gateway,
/// with the access token), the signed-in user's id, the module's texts, the cached units as a tree, live nudges,
/// one-call-at-a-time state and readable error messages.
/// </summary>
public abstract class ShiftsPageBase : ComponentBase, IAsyncDisposable
{
    [Inject] protected IHttpClientFactory HttpClients { get; set; } = default!;
    [Inject] protected MudTailRuntime MudTail { get; set; } = default!;
    [Inject] protected NavigationManager Nav { get; set; } = default!;
    [Inject] protected IStringLocalizerFactory Texts { get; set; } = default!;
    [Inject] protected AuthenticationStateProvider Auth { get; set; } = default!;
    [Inject] private IServiceProvider Services { get; set; } = default!;

    private IStringLocalizer? _l;
    private ShiftLiveTopics? _live;

    /// <summary>This module's texts (its embedded i18n/{culture}.json); the keys are the English texts.</summary>
    protected IStringLocalizer L => _l ??= Texts.Create(typeof(ShiftsPageBase));

    /// <summary>Shifts routes (/api/shifts/...) reach the pool that runs the module through the gateway.</summary>
    protected ShiftsClient Api => new(HttpClients.CreateClient(HttpClientNames.WebApi));

    protected bool Busy { get; private set; }
    protected string? Error { get; set; }

    /// <summary>The signed-in user's id (the token's <c>sub</c>): what the rota and the API's check-out rules use.</summary>
    protected string? Me { get; private set; }

    /// <summary>The Organization units the module caches (GET /units), as a tree; empty until <see cref="LoadUnitsAsync"/>.</summary>
    protected UnitTree Units { get; private set; } = UnitTree.Empty;

    protected static bool Arabic => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

    protected override async Task OnInitializedAsync()
    {
        var user = (await Auth.GetAuthenticationStateAsync()).User;
        Me = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    protected async Task LoadUnitsAsync() => Units = UnitTree.Build(await Api.ListUnitsAsync());

    protected string UnitName(Guid? id) => Units.Find(id) is { } unit ? UnitTree.NameOf(unit, Arabic) : L["an unknown unit"];

    /// <summary>
    /// The clock a shift or the planner shows: the unit's time zone (or the nearest above it), else UTC for a unit and
    /// the browser's own zone without a unit. <c>Name</c> says which, for the page.
    /// </summary>
    protected (TimeZoneInfo Zone, string Name) ClockOf(Guid? unitId)
    {
        if (unitId is null) return (TimeZoneInfo.Local, L["your browser's time"]);
        var id = Units.TimeZoneIdOf(unitId);
        var (zone, known) = ZoneTime.Find(id);
        if (known) return (zone, id!);
        return (zone, id is null ? L["UTC (the unit has no time zone)"] : L["UTC (this browser does not know {0})", id]);
    }

    // ───────── live ─────────

    /// <summary>The page's live subscriptions; <see cref="OnShiftChangedAsync"/> runs when a topic nudges.</summary>
    protected ShiftLiveTopics Live => _live ??= new ShiftLiveTopics(Services.GetService<ILiveChannel>(), () => InvokeAsync(OnShiftChangedAsync));

    /// <summary>A shift changed on one of the page's topics: reload, quietly.</summary>
    protected virtual Task OnShiftChangedAsync() => Task.CompletedTask;

    /// <summary>A reload nobody asked for: no busy state, and a failure leaves the page as it was.</summary>
    protected async Task QuietlyAsync(Func<Task> load)
    {
        if (Busy) return; // the running call loads the page again itself
        try { await load(); }
        catch (AccessTokenNotAvailableException ex) { ex.Redirect(); }
        catch (Exception e) when (e is ShiftsApiException or HttpRequestException) { Error = Describe(e); }
        StateHasChanged();
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (_live is not null) await _live.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    // ───────── calls and errors ─────────

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

    protected string Describe(Exception ex) => ex switch
    {
        ShiftFormException form => L[form.Message],
        ShiftsApiException { StatusCode: 400 } api when Mentions(api, "no tenant") =>
            L["400: no tenant. Shifts belong to a tenant: sign in to one (your organisation) to manage its shifts."],
        ShiftsApiException { StatusCode: 401 } => L["401: the gateway did not accept the access token. Sign in again."],
        ShiftsApiException { StatusCode: 403 } api =>
            $"403: {ProblemDetail(api) ?? L["your account lacks the shifts permission for this (shifts.view or shifts.manage in this tenant)."]}",
        ShiftsApiException { StatusCode: 404 } api => $"404: {ProblemDetail(api) ?? L["not found, or not visible to you."]}",
        ShiftsApiException { StatusCode: 409 } api => $"409: {ProblemDetail(api) ?? L["the shift does not allow this now. Reload and try again."]}",
        ShiftsApiException { StatusCode: 503 } api => $"503: {ProblemDetail(api) ?? L["Organization is not available. Try again later."]}",
        ShiftsApiException api => $"{api.StatusCode}: {ProblemDetail(api) ?? FirstLine(api.Response) ?? FirstLine(api.Message)}",
        HttpRequestException http => L["The gateway is not reachable ({0}).", http.Message],
        _ => ex.Message,
    };

    /// <summary>The "detail" (else "title") of a problem-details response body, if it is one.</summary>
    protected static string? ProblemDetail(ShiftsApiException api)
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

    /// <summary>A short label for a user id: the signed-in user is "you".</summary>
    protected string Who(string? userId) => userId switch
    {
        null => "",
        "shifts:clock" => L["the shift clock"],
        _ when userId == Me => L["you ({0})", Short(userId)],
        _ => userId
    };

    private static string Short(string id) => id.Length > 8 ? id[..8] + "…" : id;
}
