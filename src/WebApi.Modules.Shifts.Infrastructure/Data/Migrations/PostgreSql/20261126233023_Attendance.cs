using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace soft.square.net.WebApi.Modules.Shifts.Infrastructure.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class Attendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndedAt",
                schema: "shifts",
                table: "shifts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EndedBy",
                schema: "shifts",
                table: "shifts",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "HandoverAt",
                schema: "shifts",
                table: "shifts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HandoverBy",
                schema: "shifts",
                table: "shifts",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HandoverNote",
                schema: "shifts",
                table: "shifts",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AbsentAt",
                schema: "shifts",
                table: "shift_members",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CheckedInAt",
                schema: "shifts",
                table: "shift_members",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckedInBy",
                schema: "shifts",
                table: "shift_members",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CheckedOutAutomatically",
                schema: "shifts",
                table: "shift_members",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CheckedOutBy",
                schema: "shifts",
                table: "shift_members",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "unit_attendance",
                schema: "shifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EarlyCheckInMinutes = table.Column<int>(type: "integer", nullable: true),
                    LateMinutes = table.Column<int>(type: "integer", nullable: true),
                    AbsentMinutes = table.Column<int>(type: "integer", nullable: true),
                    OvertimeMinutes = table.Column<int>(type: "integer", nullable: true),
                    CheckoutGraceMinutes = table.Column<int>(type: "integer", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unit_attendance", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_shifts_EndedAt",
                schema: "shifts",
                table: "shifts",
                column: "EndedAt");

            migrationBuilder.CreateIndex(
                name: "IX_shifts_Published_CheckedOutAt_StartsAt",
                schema: "shifts",
                table: "shifts",
                columns: new[] { "Published", "CheckedOutAt", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_unit_attendance_TenantId",
                schema: "shifts",
                table: "unit_attendance",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "unit_attendance",
                schema: "shifts");

            migrationBuilder.DropIndex(
                name: "IX_shifts_EndedAt",
                schema: "shifts",
                table: "shifts");

            migrationBuilder.DropIndex(
                name: "IX_shifts_Published_CheckedOutAt_StartsAt",
                schema: "shifts",
                table: "shifts");

            migrationBuilder.DropColumn(
                name: "EndedAt",
                schema: "shifts",
                table: "shifts");

            migrationBuilder.DropColumn(
                name: "EndedBy",
                schema: "shifts",
                table: "shifts");

            migrationBuilder.DropColumn(
                name: "HandoverAt",
                schema: "shifts",
                table: "shifts");

            migrationBuilder.DropColumn(
                name: "HandoverBy",
                schema: "shifts",
                table: "shifts");

            migrationBuilder.DropColumn(
                name: "HandoverNote",
                schema: "shifts",
                table: "shifts");

            migrationBuilder.DropColumn(
                name: "AbsentAt",
                schema: "shifts",
                table: "shift_members");

            migrationBuilder.DropColumn(
                name: "CheckedInAt",
                schema: "shifts",
                table: "shift_members");

            migrationBuilder.DropColumn(
                name: "CheckedInBy",
                schema: "shifts",
                table: "shift_members");

            migrationBuilder.DropColumn(
                name: "CheckedOutAutomatically",
                schema: "shifts",
                table: "shift_members");

            migrationBuilder.DropColumn(
                name: "CheckedOutBy",
                schema: "shifts",
                table: "shift_members");
        }
    }
}
