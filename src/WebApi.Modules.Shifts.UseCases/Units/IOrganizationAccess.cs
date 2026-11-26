using soft.square.net.Organization.Contracts;
using OrgApi = soft.square.net.WebApi.Modules.Organization.Client;

namespace soft.square.net.WebApi.Modules.Shifts.UseCases.Units;

/// <summary>
/// How the module reaches the Organization service (implemented in Infrastructure over HTTP, setting section
/// <c>Shifts:Organization</c>). Every call carries the current tenant.
/// <list type="bullet">
/// <item>The local read API (<see cref="IUnitDirectory"/>, this deployment's replica) as the signed-in caller, or as
/// the module's own service client when there is no caller (the first fill at tenant start).</item>
/// <item>The central management API (<see cref="OrgApi.IOrganizationClient"/>) as the signed-in caller only: a unit is
/// created with the admin's own rights.</item>
/// </list>
/// </summary>
public interface IOrganizationAccess
{
    /// <summary>False when <c>Shifts:Organization:BaseUrl</c> is not set: nothing can be called.</summary>
    bool Configured { get; }

    /// <summary>True when a service client (token endpoint, client id and secret) is configured as well.</summary>
    bool CanActAsService { get; }

    IUnitDirectory DirectoryAsCaller();

    IUnitDirectory DirectoryAsService();

    OrgApi.IOrganizationClient CentralAsCaller();
}
