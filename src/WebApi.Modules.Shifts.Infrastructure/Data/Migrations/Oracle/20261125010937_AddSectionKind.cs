using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace soft.square.net.WebApi.Modules.Shifts.Infrastructure.Data.Migrations.Oracle
{
    /// <inheritdoc />
    public partial class AddSectionKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "shifts_sections",
                type: "NVARCHAR2(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Category"); // existing sections are category sections

            migrationBuilder.CreateIndex(
                name: "IX_shifts_sections_Kind",
                table: "shifts_sections",
                column: "Kind");

            migrationBuilder.CreateIndex(
                name: "IX_shifts_section_categories_CategoryId",
                table: "shifts_section_categories",
                column: "CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_shifts_sections_Kind",
                table: "shifts_sections");

            migrationBuilder.DropIndex(
                name: "IX_shifts_section_categories_CategoryId",
                table: "shifts_section_categories");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "shifts_sections");
        }
    }
}
