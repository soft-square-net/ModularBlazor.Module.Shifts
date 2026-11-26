using System.Reflection;
using Microsoft.EntityFrameworkCore;
using soft.square.net.WebApi.Contracts.Tenancy;
using soft.square.net.WebApi.Data;
using soft.square.net.WebApi.Modules.Shifts.Core.SectionAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.ShiftAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.UnitAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.Infrastructure.Data;

/// <summary>
/// The module's tenant data: schema "shifts" in each tenant's database. The entity mappings are the
/// <c>IEntityTypeConfiguration</c> classes in <c>Data/Config</c>; the migrations, one set per engine, are in
/// <c>Data/Migrations/&lt;Provider&gt;</c>. It maps the platform's integration outbox and inbox too
/// (<see cref="UsesIntegrationEvents"/>): the module publishes <c>shifts.*</c> events and follows <c>org.*</c> ones.
/// </summary>
public sealed class ShiftsDbContext(DbContextOptions<ShiftsDbContext> options, ITenantContext tenant) : TenantDbContext(options, tenant)
{
    protected override string Schema => "shifts";

    protected override bool UsesIntegrationEvents => true;

    public DbSet<Section> Sections => Set<Section>();
    public DbSet<SectionUnitMap> SectionUnitMaps => Set<SectionUnitMap>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<CachedUnit> Units => Set<CachedUnit>();

    protected override void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
}
