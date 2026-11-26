using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace soft.square.net.WebApi.Modules.Shifts.Infrastructure.Migrations.Oracle
{
    /// <inheritdoc />
    public partial class Attendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndedAt",
                table: "shifts_shifts",
                type: "TIMESTAMP(7) WITH TIME ZONE",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EndedBy",
                table: "shifts_shifts",
                type: "NVARCHAR2(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "HandoverAt",
                table: "shifts_shifts",
                type: "TIMESTAMP(7) WITH TIME ZONE",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HandoverBy",
                table: "shifts_shifts",
                type: "NVARCHAR2(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HandoverNote",
                table: "shifts_shifts",
                type: "NCLOB",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AbsentAt",
                table: "shifts_shift_members",
                type: "TIMESTAMP(7) WITH TIME ZONE",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CheckedInAt",
                table: "shifts_shift_members",
                type: "TIMESTAMP(7) WITH TIME ZONE",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckedInBy",
                table: "shifts_shift_members",
                type: "NVARCHAR2(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CheckedOutAutomatically",
                table: "shifts_shift_members",
                type: "BOOLEAN",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CheckedOutBy",
                table: "shifts_shift_members",
                type: "NVARCHAR2(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "shifts_unit_attendance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    TenantId = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: false),
                    EarlyCheckInMinutes = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    LateMinutes = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    AbsentMinutes = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    OvertimeMinutes = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    CheckoutGraceMinutes = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    UpdatedBy = table.Column<string>(type: "NVARCHAR2(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shifts_unit_attendance", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_shifts_shifts_EndedAt",
                table: "shifts_shifts",
                column: "EndedAt");

            migrationBuilder.CreateIndex(
                name: "IX_shifts_shifts_Published_CheckedOutAt_StartsAt",
                table: "shifts_shifts",
                columns: new[] { "Published", "CheckedOutAt", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_shifts_unit_attendance_TenantId",
                table: "shifts_unit_attendance",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shifts_unit_attendance");

            migrationBuilder.DropIndex(
                name: "IX_shifts_shifts_EndedAt",
                table: "shifts_shifts");

            migrationBuilder.DropIndex(
                name: "IX_shifts_shifts_Published_CheckedOutAt_StartsAt",
                table: "shifts_shifts");

            migrationBuilder.DropColumn(
                name: "EndedAt",
                table: "shifts_shifts");

            migrationBuilder.DropColumn(
                name: "EndedBy",
                table: "shifts_shifts");

            migrationBuilder.DropColumn(
                name: "HandoverAt",
                table: "shifts_shifts");

            migrationBuilder.DropColumn(
                name: "HandoverBy",
                table: "shifts_shifts");

            migrationBuilder.DropColumn(
                name: "HandoverNote",
                table: "shifts_shifts");

            migrationBuilder.DropColumn(
                name: "AbsentAt",
                table: "shifts_shift_members");

            migrationBuilder.DropColumn(
                name: "CheckedInAt",
                table: "shifts_shift_members");

            migrationBuilder.DropColumn(
                name: "CheckedInBy",
                table: "shifts_shift_members");

            migrationBuilder.DropColumn(
                name: "CheckedOutAutomatically",
                table: "shifts_shift_members");

            migrationBuilder.DropColumn(
                name: "CheckedOutBy",
                table: "shifts_shift_members");
        }
    }
}
