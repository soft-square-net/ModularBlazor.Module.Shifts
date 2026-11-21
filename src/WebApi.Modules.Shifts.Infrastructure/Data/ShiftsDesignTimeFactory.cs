using Microsoft.EntityFrameworkCore.Design;
using soft.square.net.WebApi.Data;

namespace soft.square.net.WebApi.Modules.Shifts.Infrastructure.Data;

/// <summary>
/// Design time, one migration set per engine (Data/Migrations/&lt;Provider&gt;): add migrations with the platform's
/// src/WebApi/scripts/Add-Migration.ps1 -Project &lt;this project&gt; -MigrationsDir Data/Migrations.
/// </summary>
internal sealed class ShiftsDesignTimeFactory : IDesignTimeDbContextFactory<ShiftsDbContext>
{
    public ShiftsDbContext CreateDbContext(string[] args) =>
        new(TenantDbDesignTime.Options<ShiftsDbContext>(args), TenantDbDesignTime.Tenant);
}
