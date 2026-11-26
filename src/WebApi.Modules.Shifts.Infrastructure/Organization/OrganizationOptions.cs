namespace soft.square.net.WebApi.Modules.Shifts.Infrastructure.Organization;

/// <summary>
/// How the module reaches Organization (module settings, section <c>Shifts:Organization</c>).
/// </summary>
public sealed class OrganizationOptions
{
    /// <summary>
    /// The address that serves this deployment's <c>/api/organization/local/*</c> (the gateway, or an internal address
    /// that reaches the pool running Organization), e.g. <c>https://gateway/</c>. Empty: Organization is not called;
    /// the unit cache then fills from events only.
    /// </summary>
    public string BaseUrl { get; set; } = "";

    /// <summary>The address of the central management API (<c>/api/organization/*</c>). Empty: <see cref="BaseUrl"/>.</summary>
    public string CentralUrl { get; set; } = "";

    /// <summary>
    /// The module's service client (client credentials), for reads without a signed-in caller: the first fill of the
    /// unit cache at tenant start. Leave <see cref="TokenEndpoint"/> empty to skip that fill.
    /// </summary>
    public string TokenEndpoint { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string ClientScope { get; set; } = "";

    public bool Configured => !string.IsNullOrWhiteSpace(BaseUrl);

    public bool CanActAsService =>
        Configured && !string.IsNullOrWhiteSpace(TokenEndpoint) && !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
