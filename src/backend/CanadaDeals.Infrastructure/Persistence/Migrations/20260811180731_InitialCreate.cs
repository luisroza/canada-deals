using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanadaDeals.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "Brands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Slug = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Slug = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MerchantPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    AllowPriceStorage = table.Column<int>(type: "integer", nullable: false),
                    AllowPriceHistory = table.Column<int>(type: "integer", nullable: false),
                    AllowImageCaching = table.Column<int>(type: "integer", nullable: false),
                    AllowMetadataCaching = table.Column<int>(type: "integer", nullable: false),
                    PriceMaxAgeHours = table.Column<int>(type: "integer", nullable: true),
                    AllowedComparison = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    RequiredAttribution = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DisclosureText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LinkExpiration = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RawRetentionDays = table.Column<int>(type: "integer", nullable: true),
                    DataResidencyNotes = table.Column<string>(type: "text", nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MerchantPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Retailers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Retailers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    ModelNumber = table.Column<string>(type: "text", nullable: true),
                    ManufacturerPartNumber = table.Column<string>(type: "text", nullable: true),
                    Gtin = table.Column<string>(type: "text", nullable: true),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantAttributesJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RetailerListings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    RetailerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalListingId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    RetailerSku = table.Column<string>(type: "text", nullable: true),
                    OriginalTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ProductUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ApprovedAffiliateDestinationReference = table.Column<string>(type: "text", nullable: true),
                    Seller = table.Column<string>(type: "text", nullable: true),
                    IsMarketplaceSeller = table.Column<bool>(type: "boolean", nullable: true),
                    Condition = table.Column<int>(type: "integer", nullable: false),
                    VariantAttributesJson = table.Column<string>(type: "jsonb", nullable: false),
                    PackQuantity = table.Column<int>(type: "integer", nullable: true),
                    BundleContents = table.Column<string>(type: "text", nullable: true),
                    RegionAvailabilityContext = table.Column<string>(type: "text", nullable: true),
                    OnlineAvailability = table.Column<int>(type: "integer", nullable: false),
                    ShippingContext = table.Column<string>(type: "text", nullable: true),
                    ExternalIdentifiersJson = table.Column<string>(type: "jsonb", nullable: false),
                    SourceObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FetchedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Freshness = table.Column<int>(type: "integer", nullable: false),
                    Evidence = table.Column<int>(type: "integer", nullable: false),
                    History = table.Column<int>(type: "integer", nullable: false),
                    MatchState = table.Column<int>(type: "integer", nullable: false),
                    CurrentPriceAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    CurrentPriceCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    MerchantPolicyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetailerListings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RetailerListings_MerchantPolicies_MerchantPolicyId",
                        column: x => x.MerchantPolicyId,
                        principalTable: "MerchantPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RetailerListings_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RetailerListings_Retailers_RetailerId",
                        column: x => x.RetailerId,
                        principalTable: "Retailers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PriceObservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RetailerListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FetchedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsPermitted = table.Column<bool>(type: "boolean", nullable: false),
                    SourceHash = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceObservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceObservations_RetailerListings_RetailerListingId",
                        column: x => x.RetailerListingId,
                        principalTable: "RetailerListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Brands_Slug",
                table: "Brands",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                table: "Categories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MerchantPolicies_SourceKey",
                table: "MerchantPolicies",
                column: "SourceKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceObservations_RetailerListingId_ObservedAt_SourceHash",
                table: "PriceObservations",
                columns: new[] { "RetailerListingId", "ObservedAt", "SourceHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_BrandId_ModelNumber",
                table: "Products",
                columns: new[] { "BrandId", "ModelNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Gtin",
                table: "Products",
                column: "Gtin");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Slug",
                table: "Products",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RetailerListings_MerchantPolicyId",
                table: "RetailerListings",
                column: "MerchantPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_RetailerListings_ProductId",
                table: "RetailerListings",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_RetailerListings_RetailerId_ExternalListingId",
                table: "RetailerListings",
                columns: new[] { "RetailerId", "ExternalListingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Retailers_Key",
                table: "Retailers",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceObservations");

            migrationBuilder.DropTable(
                name: "RetailerListings");

            migrationBuilder.DropTable(
                name: "MerchantPolicies");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Retailers");

            migrationBuilder.DropTable(
                name: "Brands");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
