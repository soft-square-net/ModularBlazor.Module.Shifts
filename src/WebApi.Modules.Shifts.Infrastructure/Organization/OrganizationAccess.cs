using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using soft.square.net.Organization.Contracts;
using soft.square.net.WebApi.Contracts.Tenancy;
using soft.square.net.WebApi.Modules.Shifts.UseCases.Units;
using OrgApi = soft.square.net.WebApi.Modules.Organization.Client;

namespace soft.square.net.WebApi.Modules.Shifts.Infrastructure.Organization;

/// <summary>
/// The module's HTTP connection to Organization (a singleton of the module's container, disposed on unload, so no
/// socket outlives the module), and the service client's tokens (client credentials, cached until shortly before they
/// expire).
/// </summary>
public sealed class OrganizationHttp(IOptions<OrganizationOptions> options, TimeProvider clock) : IDisposable
{
    private readonly SocketsHttpHandler _sockets = new() { PooledConnectionLifetime = TimeSpan.FromMinutes(5) };
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _token;
    private DateTimeOffset _expires;

    public OrganizationOptions Options => options.Value;

    /// <summary>A client for <paramref name="baseUrl"/> that sends the tenant and the given Authorization header.</summary>
    public HttpClient Client(string baseUrl, string tenantId, Func<CancellationToken, Task<string?>> authorization) =>
        new(new Headers(tenantId, authorization, this) { InnerHandler = _sockets }, disposeHandler: false)
        {
            BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/"),
            Timeout = TimeSpan.FromSeconds(60)
        };

    /// <summary>The service client's bearer header, or null when no service client is configured.</summary>
    public async Task<string?> ServiceAuthorizationAsync(CancellationToken ct)
    {
        var o = Options;
        if (!o.CanActAsService) return null;
        if (_token is { } cached && clock.GetUtcNow() < _expires) return "Bearer " + cached;
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_token is { } again && clock.GetUtcNow() < _expires) return "Bearer " + again;
            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = o.ClientId,
                ["client_secret"] = o.ClientSecret
            };
            if (!string.IsNullOrWhiteSpace(o.ClientScope)) form["scope"] = o.ClientScope;
            using var http = new HttpClient(_sockets, disposeHandler: false) { Timeout = TimeSpan.FromSeconds(30) };
            using var response = await http.PostAsync(o.TokenEndpoint, new FormUrlEncodedContent(form), ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"The token endpoint refused the Shifts service client ({(int)response.StatusCode}).", null, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<TokenResponse>(ct).ConfigureAwait(false)
                       ?? throw new HttpRequestException("The token endpoint returned no token.");
            _token = body.AccessToken;
            _expires = clock.GetUtcNow() + TimeSpan.FromSeconds(Math.Max(30, body.ExpiresIn - 60));
            return "Bearer " + _token;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        _sockets.Dispose();
        _gate.Dispose();
    }

    private sealed class Headers(string tenantId, Func<CancellationToken, Task<string?>> authorization, OrganizationHttp owner) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            request.Headers.TryAddWithoutValidation(TenancyNames.Header, tenantId);
            if (await authorization(ct).ConfigureAwait(false) is { Length: > 0 } header)
                request.Headers.Authorization = AuthenticationHeaderValue.Parse(header);
            var response = await base.SendAsync(request, ct).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.Unauthorized) owner._token = null; // fetch a new service token next time
            return response;
        }
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}

/// <summary>
/// <see cref="IOrganizationAccess"/> for the scope's tenant: <see cref="HttpUnitDirectory"/> over
/// <c>{BaseUrl}/api/organization/</c>, and Organization's typed client over <c>CentralUrl</c> (or <c>BaseUrl</c>).
/// As the caller, the request's own Authorization header is forwarded.
/// </summary>
public sealed class OrganizationAccess(OrganizationHttp http, ITenantContext tenant, IHttpContextAccessor accessor) : IOrganizationAccess
{
    private const string ModuleRoute = "api/organization/";

    public bool Configured => http.Options.Configured;

    public bool CanActAsService => http.Options.CanActAsService;

    public IUnitDirectory DirectoryAsCaller() => new HttpUnitDirectory(Client(Root(http.Options.BaseUrl) + ModuleRoute, CallerAuthorization));

    public IUnitDirectory DirectoryAsService() => new HttpUnitDirectory(Client(Root(http.Options.BaseUrl) + ModuleRoute, http.ServiceAuthorizationAsync));

    public OrgApi.IOrganizationClient CentralAsCaller() =>
        new OrgApi.OrganizationClient(Client(Root(string.IsNullOrWhiteSpace(http.Options.CentralUrl) ? http.Options.BaseUrl : http.Options.CentralUrl),
            CallerAuthorization));

    private HttpClient Client(string baseUrl, Func<CancellationToken, Task<string?>> authorization)
    {
        if (!Configured) throw new InvalidOperationException("Shifts:Organization:BaseUrl is not set.");
        return http.Client(baseUrl, tenant.Tenant?.Id ?? throw new InvalidOperationException("Organization is called in a tenant scope only."), authorization);
    }

    private Task<string?> CallerAuthorization(CancellationToken ct) =>
        Task.FromResult(accessor.HttpContext?.Request.Headers.Authorization.ToString() is { Length: > 0 } header ? header : null);

    private static string Root(string url) => url.EndsWith('/') ? url : url + "/";
}
