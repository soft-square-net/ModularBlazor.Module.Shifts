using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using soft.square.net.WebApi.Modules.Shifts.Core.RoleAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.Infrastructure.Data.Config;

public sealed class ShiftRoleDefinitionConfiguration : IEntityTypeConfiguration<ShiftRoleDefinition>
{
    public void Configure(EntityTypeBuilder<ShiftRoleDefinition> role)
    {
        role.ToTable("shift_roles");
        role.Property(r => r.Key).HasMaxLength(ShiftRoleDefinition.KeyLength);
        role.Property(r => r.NameEn).HasMaxLength(ShiftRoleDefinition.NameLength);
        role.Property(r => r.NameAr).HasMaxLength(ShiftRoleDefinition.NameLength);
        role.Property(r => r.Permissions).HasConversion(StringListConversion.Converter, StringListConversion.Comparer)
            .HasMaxLength(DataSchemaConstants.ListLength);
        // One definition of a key per unit and tenant (the use case also checks the tenant-wide ones: engines differ on NULLs).
        role.HasIndex(r => new { r.TenantId, r.UnitId, r.Key }).IsUnique();
    }
}
