using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace CanadaDeals.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPostgresProductSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedManufacturerPartNumber",
                table: "Products",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedModelNumber",
                table: "Products",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SearchDocument",
                table: "Products",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Products",
                type: "tsvector",
                nullable: true)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "SearchDocument" });

            migrationBuilder.Sql(
                """
                UPDATE "Products" AS p
                SET
                    "SearchDocument" = concat_ws(
                        ' ',
                        p."Title",
                        b."Name",
                        c."Name",
                        p."ModelNumber",
                        p."ManufacturerPartNumber",
                        p."Gtin"),
                    "NormalizedModelNumber" = nullif(
                        upper(regexp_replace(coalesce(p."ModelNumber", ''), '[^[:alnum:]]', '', 'g')),
                        ''),
                    "NormalizedManufacturerPartNumber" = nullif(
                        upper(regexp_replace(coalesce(p."ManufacturerPartNumber", ''), '[^[:alnum:]]', '', 'g')),
                        '')
                FROM "Brands" AS b, "Categories" AS c
                WHERE p."BrandId" = b."Id"
                  AND p."CategoryId" = c."Id";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_RetailerListings_CurrentPriceAmount",
                table: "RetailerListings",
                column: "CurrentPriceAmount");

            migrationBuilder.CreateIndex(
                name: "IX_RetailerListings_OnlineAvailability_MatchState",
                table: "RetailerListings",
                columns: new[] { "OnlineAvailability", "MatchState" });

            migrationBuilder.CreateIndex(
                name: "IX_RetailerListings_SourceObservedAt",
                table: "RetailerListings",
                column: "SourceObservedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Products_NormalizedManufacturerPartNumber",
                table: "Products",
                column: "NormalizedManufacturerPartNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Products_NormalizedModelNumber",
                table: "Products",
                column: "NormalizedModelNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Products_SearchDocument",
                table: "Products",
                column: "SearchDocument")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_SearchVector",
                table: "Products",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RetailerListings_CurrentPriceAmount",
                table: "RetailerListings");

            migrationBuilder.DropIndex(
                name: "IX_RetailerListings_OnlineAvailability_MatchState",
                table: "RetailerListings");

            migrationBuilder.DropIndex(
                name: "IX_RetailerListings_SourceObservedAt",
                table: "RetailerListings");

            migrationBuilder.DropIndex(
                name: "IX_Products_NormalizedManufacturerPartNumber",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_NormalizedModelNumber",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_SearchDocument",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_SearchVector",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "NormalizedManufacturerPartNumber",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "NormalizedModelNumber",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SearchDocument",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "Products");
        }
    }
}
