using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanadaDeals.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminCatalogWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OfferValidUntil",
                table: "RetailerListings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "Brands",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_RetailerListings_IsEnabled_OfferValidUntil",
                table: "RetailerListings",
                columns: new[] { "IsEnabled", "OfferValidUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_Brands_IsEnabled_Name",
                table: "Brands",
                columns: new[] { "IsEnabled", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RetailerListings_IsEnabled_OfferValidUntil",
                table: "RetailerListings");

            migrationBuilder.DropIndex(
                name: "IX_Brands_IsEnabled_Name",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "OfferValidUntil",
                table: "RetailerListings");

            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "Brands");
        }
    }
}
