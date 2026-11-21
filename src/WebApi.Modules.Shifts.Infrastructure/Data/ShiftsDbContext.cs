using System.Reflection;
using Microsoft.EntityFrameworkCore;
using soft.square.net.WebApi.Contracts.Tenancy;
using soft.square.net.WebApi.Data;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.Infrastructure.Data;

/// <summary>
/// The module's tenant data: schema "shifts" in each tenant's database. The entity mappings are the
/// <c>IEntityTypeConfiguration</c> classes in <c>Data/Config</c>; the migrations, one set per engine, are in
/// <c>Data/Migrations/&lt;Provider&gt;</c>.
/// </summary>
public sealed class ShiftsDbContext(DbContextOptions<ShiftsDbContext> options, ITenantContext tenant) : TenantDbContext(options, tenant)
{
    protected override string Schema => "shifts";

    public DbSet<Section> Sections => Set<Section>();
    public DbSet<Shift> Shifts => Set<Shift>();

    protected override void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
}
