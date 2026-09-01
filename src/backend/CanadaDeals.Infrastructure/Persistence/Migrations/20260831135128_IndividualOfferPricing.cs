using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanadaDeals.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IndividualOfferPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RetailerListings_IsEnabled_OfferValidUntil",
                table: "RetailerListings");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OfferValidFrom",
                table: "RetailerListings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RegularPriceAmount",
                table: "RetailerListings",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegularPriceCurrency",
                table: "RetailerListings",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegularPriceEvidenceReference",
                table: "RetailerListings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RegularPriceObservedAt",
                table: "RetailerListings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SavedOffers",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RetailerListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedOffers", x => new { x.UserId, x.RetailerListingId });
                    table.ForeignKey(
                        name: "FK_SavedOffers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavedOffers_RetailerListings_RetailerListingId",
                        column: x => x.RetailerListingId,
                        principalTable: "RetailerListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SavedOfferMigrationOrphans",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedOfferMigrationOrphans", x => new { x.UserId, x.ProductId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_RetailerListings_IsEnabled_OfferValidFrom_OfferValidUntil",
                table: "RetailerListings",
                columns: new[] { "IsEnabled", "OfferValidFrom", "OfferValidUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedOffers_RetailerListingId",
                table: "SavedOffers",
                column: "RetailerListingId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedOffers_UserId_CreatedAt",
                table: "SavedOffers",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.Sql("""
                INSERT INTO "SavedOfferMigrationOrphans" ("UserId", "ProductId", "CreatedAt", "Reason")
                SELECT saved."UserId", saved."ProductId", saved."CreatedAt", 'NO_RETAILER_LISTING'
                FROM "SavedProducts" AS saved
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "RetailerListings" AS listing
                    WHERE listing."ProductId" = saved."ProductId"
                );

                INSERT INTO "SavedOffers" ("UserId", "RetailerListingId", "CreatedAt")
                SELECT saved."UserId", selected."Id", saved."CreatedAt"
                FROM "SavedProducts" AS saved
                JOIN LATERAL (
                    SELECT listing."Id"
                    FROM "RetailerListings" AS listing
                    WHERE listing."ProductId" = saved."ProductId"
                    ORDER BY listing."IsEnabled" DESC, listing."SourceObservedAt" DESC NULLS LAST, listing."Id"
                    LIMIT 1
                ) AS selected ON TRUE;
                """);

            migrationBuilder.DropTable(
                name: "SavedProducts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RetailerListings_IsEnabled_OfferValidFrom_OfferValidUntil",
                table: "RetailerListings");

            migrationBuilder.DropColumn(
                name: "OfferValidFrom",
                table: "RetailerListings");

            migrationBuilder.DropColumn(
                name: "RegularPriceAmount",
                table: "RetailerListings");

            migrationBuilder.DropColumn(
                name: "RegularPriceCurrency",
                table: "RetailerListings");

            migrationBuilder.DropColumn(
                name: "RegularPriceEvidenceReference",
                table: "RetailerListings");

            migrationBuilder.DropColumn(
                name: "RegularPriceObservedAt",
                table: "RetailerListings");

            migrationBuilder.CreateTable(
                name: "SavedProducts",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedProducts", x => new { x.UserId, x.ProductId });
                    table.ForeignKey(
                        name: "FK_SavedProducts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavedProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RetailerListings_IsEnabled_OfferValidUntil",
                table: "RetailerListings",
                columns: new[] { "IsEnabled", "OfferValidUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedProducts_ProductId",
                table: "SavedProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedProducts_UserId_CreatedAt",
                table: "SavedProducts",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.Sql("""
                INSERT INTO "SavedProducts" ("UserId", "ProductId", "CreatedAt")
                SELECT saved."UserId", listing."ProductId", MIN(saved."CreatedAt")
                FROM "SavedOffers" AS saved
                INNER JOIN "RetailerListings" AS listing ON listing."Id" = saved."RetailerListingId"
                GROUP BY saved."UserId", listing."ProductId"
                ON CONFLICT ("UserId", "ProductId") DO NOTHING;

                INSERT INTO "SavedProducts" ("UserId", "ProductId", "CreatedAt")
                SELECT orphan."UserId", orphan."ProductId", orphan."CreatedAt"
                FROM "SavedOfferMigrationOrphans" AS orphan
                ON CONFLICT ("UserId", "ProductId") DO UPDATE
                SET "CreatedAt" = LEAST("SavedProducts"."CreatedAt", EXCLUDED."CreatedAt");
                """);

            migrationBuilder.DropTable(
                name: "SavedOffers");

            migrationBuilder.DropTable(
                name: "SavedOfferMigrationOrphans");
        }
    }
}
