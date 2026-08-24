using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanadaDeals.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminCategoryAndRetailerManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "Categories",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_IsEnabled_Name",
                table: "Categories",
                columns: new[] { "IsEnabled", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categories_IsEnabled_Name",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "Categories");
        }
    }
}
