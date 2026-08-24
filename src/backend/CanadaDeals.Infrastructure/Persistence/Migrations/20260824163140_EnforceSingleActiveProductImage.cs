using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanadaDeals.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSingleActiveProductImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductImages_ContentHash",
                table: "ProductImages");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId",
                table: "ProductImages",
                column: "ProductId",
                unique: true,
                filter: "\"State\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId_ContentHash",
                table: "ProductImages",
                columns: new[] { "ProductId", "ContentHash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductImages_ProductId",
                table: "ProductImages");

            migrationBuilder.DropIndex(
                name: "IX_ProductImages_ProductId_ContentHash",
                table: "ProductImages");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ContentHash",
                table: "ProductImages",
                column: "ContentHash");
        }
    }
}
