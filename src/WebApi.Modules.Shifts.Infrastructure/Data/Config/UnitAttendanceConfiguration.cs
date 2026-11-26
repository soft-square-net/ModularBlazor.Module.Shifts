using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using soft.square.net.WebApi.Modules.Shifts.Core.UnitAggregate;

namespace soft.square.net.WebApi.Modules.Shifts.Infrastructure.Data.Config;

public sealed class UnitAttendanceConfiguration : IEntityTypeConfiguration<UnitAttendance>
{
    public void Configure(EntityTypeBuilder<UnitAttendance> unit)
    {
        unit.ToTable("unit_attendance");
        unit.Property(u => u.Id).ValueGeneratedNever(); // Organization's unit id
        unit.Property(u => u.UpdatedBy).HasMaxLength(DataSchemaConstants.UserIdLength);
        unit.Ignore(u => u.IsEmpty);
    }
}
