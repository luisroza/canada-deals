using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanadaDeals.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRakutenConnector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AllowAffiliateLinks",
                table: "MerchantPolicies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "RakutenAdvertiserCapabilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdvertiserMid = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AdvertiserName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    AdvertiserUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AdvertiserStatus = table.Column<int>(type: "integer", nullable: false),
                    PartnershipStatus = table.Column<int>(type: "integer", nullable: false),
                    PartnershipApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PartnershipStatusUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ShipsToJson = table.Column<string>(type: "jsonb", nullable: false),
                    ProductFeedAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    DeepLinksAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    CanadaRelevant = table.Column<bool>(type: "boolean", nullable: true),
                    RetailerId = table.Column<Guid>(type: "uuid", nullable: true),
                    MerchantPolicyId = table.Column<Guid>(type: "uuid", nullable: true),
                    AffiliateEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CatalogEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CapabilityCheckedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RakutenAdvertiserCapabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RakutenAdvertiserCapabilities_MerchantPolicies_MerchantPoli~",
                        column: x => x.MerchantPolicyId,
                        principalTable: "MerchantPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RakutenAdvertiserCapabilities_Retailers_RetailerId",
                        column: x => x.RetailerId,
                        principalTable: "Retailers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RakutenImportRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdvertiserMid = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DryRun = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PagesFetched = table.Column<int>(type: "integer", nullable: false),
                    RecordsReceived = table.Column<int>(type: "integer", nullable: false),
                    ListingsCreated = table.Column<int>(type: "integer", nullable: false),
                    ListingsUpdated = table.Column<int>(type: "integer", nullable: false),
                    ObservationsCreated = table.Column<int>(type: "integer", nullable: false),
                    Skipped = table.Column<int>(type: "integer", nullable: false),
                    PolicyBlocked = table.Column<int>(type: "integer", nullable: false),
                    ReviewCandidates = table.Column<int>(type: "integer", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RakutenImportRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RakutenSourceMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdvertiserMid = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SourceListingKey = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    RetailerListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RakutenSourceMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RakutenSourceMappings_RetailerListings_RetailerListingId",
                        column: x => x.RetailerListingId,
                        principalTable: "RetailerListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RakutenAdvertiserCapabilities_AdvertiserMid",
                table: "RakutenAdvertiserCapabilities",
                column: "AdvertiserMid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RakutenAdvertiserCapabilities_MerchantPolicyId",
                table: "RakutenAdvertiserCapabilities",
                column: "MerchantPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_RakutenAdvertiserCapabilities_PartnershipStatus_AdvertiserS~",
                table: "RakutenAdvertiserCapabilities",
                columns: new[] { "PartnershipStatus", "AdvertiserStatus", "CapabilityCheckedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RakutenAdvertiserCapabilities_RetailerId",
                table: "RakutenAdvertiserCapabilities",
                column: "RetailerId");

            migrationBuilder.CreateIndex(
                name: "IX_RakutenImportRuns_AdvertiserMid_StartedAt",
                table: "RakutenImportRuns",
                columns: new[] { "AdvertiserMid", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RakutenImportRuns_Status_StartedAt",
                table: "RakutenImportRuns",
                columns: new[] { "Status", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RakutenSourceMappings_AdvertiserMid_SourceListingKey",
                table: "RakutenSourceMappings",
                columns: new[] { "AdvertiserMid", "SourceListingKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RakutenSourceMappings_RetailerListingId",
                table: "RakutenSourceMappings",
                column: "RetailerListingId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RakutenAdvertiserCapabilities");

            migrationBuilder.DropTable(
                name: "RakutenImportRuns");

            migrationBuilder.DropTable(
                name: "RakutenSourceMappings");

            migrationBuilder.DropColumn(
                name: "AllowAffiliateLinks",
                table: "MerchantPolicies");
        }
    }
}
