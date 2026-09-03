using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanadaDeals.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiNetworkCatalogIngestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CatalogImportRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ProviderAdvertiserId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    RetailerId = table.Column<Guid>(type: "uuid", nullable: true),
                    DryRun = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PagesFetched = table.Column<int>(type: "integer", nullable: false),
                    RecordsReceived = table.Column<int>(type: "integer", nullable: false),
                    ValidRecords = table.Column<int>(type: "integer", nullable: false),
                    CadRecords = table.Column<int>(type: "integer", nullable: false),
                    MappedRecords = table.Column<int>(type: "integer", nullable: false),
                    UnmappedRecords = table.Column<int>(type: "integer", nullable: false),
                    ListingsCreated = table.Column<int>(type: "integer", nullable: false),
                    ListingsUpdated = table.Column<int>(type: "integer", nullable: false),
                    ObservationsCreated = table.Column<int>(type: "integer", nullable: false),
                    Skipped = table.Column<int>(type: "integer", nullable: false),
                    PolicyBlocked = table.Column<int>(type: "integer", nullable: false),
                    ReviewCandidates = table.Column<int>(type: "integer", nullable: false),
                    UnsupportedCurrency = table.Column<int>(type: "integer", nullable: false),
                    Invalid = table.Column<int>(type: "integer", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogImportRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogImportRuns_Retailers_RetailerId",
                        column: x => x.RetailerId,
                        principalTable: "Retailers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CatalogMerchantSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ProviderAdvertiserId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CatalogId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    RelationshipStatus = table.Column<int>(type: "integer", nullable: false),
                    CatalogAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    AffiliateAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    CanadaRelevant = table.Column<bool>(type: "boolean", nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    RetailerId = table.Column<Guid>(type: "uuid", nullable: true),
                    MerchantPolicyId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    AllowedDestinationHostsJson = table.Column<string>(type: "jsonb", nullable: false),
                    CatalogEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    DiscoveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogMerchantSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogMerchantSources_Categories_DefaultCategoryId",
                        column: x => x.DefaultCategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CatalogMerchantSources_MerchantPolicies_MerchantPolicyId",
                        column: x => x.MerchantPolicyId,
                        principalTable: "MerchantPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CatalogMerchantSources_Retailers_RetailerId",
                        column: x => x.RetailerId,
                        principalTable: "Retailers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CatalogSourceMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ProviderAdvertiserId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    SourceListingKey = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    RetailerListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogSourceMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogSourceMappings_RetailerListings_RetailerListingId",
                        column: x => x.RetailerListingId,
                        principalTable: "RetailerListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogImportRuns_Provider_ProviderAdvertiserId_StartedAt",
                table: "CatalogImportRuns",
                columns: new[] { "Provider", "ProviderAdvertiserId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogImportRuns_RetailerId",
                table: "CatalogImportRuns",
                column: "RetailerId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogImportRuns_Status_StartedAt",
                table: "CatalogImportRuns",
                columns: new[] { "Status", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMerchantSources_DefaultCategoryId",
                table: "CatalogMerchantSources",
                column: "DefaultCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMerchantSources_MerchantPolicyId",
                table: "CatalogMerchantSources",
                column: "MerchantPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMerchantSources_Provider_ProviderAdvertiserId_Catalo~",
                table: "CatalogMerchantSources",
                columns: new[] { "Provider", "ProviderAdvertiserId", "CatalogId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMerchantSources_RetailerId",
                table: "CatalogMerchantSources",
                column: "RetailerId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogMerchantSources_State_UpdatedAt",
                table: "CatalogMerchantSources",
                columns: new[] { "State", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogSourceMappings_Provider_ProviderAdvertiserId_SourceL~",
                table: "CatalogSourceMappings",
                columns: new[] { "Provider", "ProviderAdvertiserId", "SourceListingKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatalogSourceMappings_RetailerListingId",
                table: "CatalogSourceMappings",
                column: "RetailerListingId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogImportRuns");

            migrationBuilder.DropTable(
                name: "CatalogMerchantSources");

            migrationBuilder.DropTable(
                name: "CatalogSourceMappings");
        }
    }
}
