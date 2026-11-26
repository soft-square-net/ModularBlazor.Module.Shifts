using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using soft.square.net.WebApi.Modules.Shifts.Core.RoleAggregate;
using soft.square.net.WebApi.Modules.Shifts.Core.TemplateAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.Infrastructure.Data.Config;

public sealed class ShiftTemplateConfiguration : IEntityTypeConfiguration<ShiftTemplate>
{
    public void Configure(EntityTypeBuilder<ShiftTemplate> template)
    {
        template.ToTable("shift_templates");
        template.Property(t => t.Name).HasMaxLength(ShiftTemplate.NameLength);
        // Ticks: the same integer column on every engine (time-of-day and interval types differ between them).
        template.Property(t => t.StartLocal).HasConversion(v => v.Ticks, v => new TimeOnly(v));
        template.Property(t => t.Duration).HasConversion(v => v.Ticks, v => TimeSpan.FromTicks(v));
        template.HasIndex(t => t.UnitId);
        template.HasMany(t => t.Slots).WithOne().HasForeignKey("TemplateId").OnDelete(DeleteBehavior.Cascade);
        template.Navigation(t => t.Slots).UsePropertyAccessMode(PropertyAccessMode.Field);
        template.HasMany(t => t.DefaultMembers).WithOne().HasForeignKey("TemplateId").OnDelete(DeleteBehavior.Cascade);
        template.Navigation(t => t.DefaultMembers).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ShiftTemplateSlotConfiguration : IEntityTypeConfiguration<ShiftTemplateSlot>
{
    public void Configure(EntityTypeBuilder<ShiftTemplateSlot> slot)
    {
        slot.ToTable("shift_template_slots");
        slot.Property(s => s.RoleKey).HasMaxLength(ShiftRoleDefinition.KeyLength);
    }
}

public sealed class ShiftTemplateMemberConfiguration : IEntityTypeConfiguration<ShiftTemplateMember>
{
    public void Configure(EntityTypeBuilder<ShiftTemplateMember> member)
    {
        member.ToTable("shift_template_members");
        member.Property(m => m.UserId).HasMaxLength(DataSchemaConstants.UserIdLength);
        member.Property(m => m.RoleKey).HasMaxLength(ShiftRoleDefinition.KeyLength);
    }
}
