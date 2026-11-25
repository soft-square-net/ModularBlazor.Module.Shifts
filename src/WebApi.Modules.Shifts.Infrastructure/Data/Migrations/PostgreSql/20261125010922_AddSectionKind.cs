using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace soft.square.net.WebApi.Modules.Shifts.Infrastructure.Data.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class AddSectionKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Kind",
                schema: "shifts",
                table: "sections",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Category"); // existing sections are category sections

            migrationBuilder.CreateIndex(
                name: "IX_sections_Kind",
                schema: "shifts",
                table: "sections",
                column: "Kind");

            migrationBuilder.CreateIndex(
                name: "IX_section_categories_CategoryId",
                schema: "shifts",
                table: "section_categories",
                column: "CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_sections_Kind",
                schema: "shifts",
                table: "sections");

            migrationBuilder.DropIndex(
                name: "IX_section_categories_CategoryId",
                schema: "shifts",
                table: "section_categories");

            migrationBuilder.DropColumn(
                name: "Kind",
                schema: "shifts",
                table: "sections");
        }
    }
}
