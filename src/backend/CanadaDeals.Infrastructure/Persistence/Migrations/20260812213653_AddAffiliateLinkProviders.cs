using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanadaDeals.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAffiliateLinkProviders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AffiliatePrograms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RetailerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ProviderProgramId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    MediaPropertyId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    ProviderLinkReference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    AllowsDeepLinking = table.Column<bool>(type: "boolean", nullable: true),
                    DestinationDomainsJson = table.Column<string>(type: "jsonb", nullable: false),
                    TrackingDomainsJson = table.Column<string>(type: "jsonb", nullable: false),
                    RelationshipEvidenceReference = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RelationshipValidatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AffiliatePrograms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AffiliatePrograms_Retailers_RetailerId",
                        column: x => x.RetailerId,
                        principalTable: "Retailers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AffiliateLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RetailerListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    AffiliateProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    TrackingUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DestinationUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ProviderReference = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastValidatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevalidateAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AffiliateLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AffiliateLinks_AffiliatePrograms_AffiliateProgramId",
                        column: x => x.AffiliateProgramId,
                        principalTable: "AffiliatePrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AffiliateLinks_RetailerListings_RetailerListingId",
                        column: x => x.RetailerListingId,
                        principalTable: "RetailerListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClickEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AffiliateLinkId = table.Column<Guid>(type: "uuid", nullable: false),
                    RetailerListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Placement = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClickEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClickEvents_AffiliateLinks_AffiliateLinkId",
                        column: x => x.AffiliateLinkId,
                        principalTable: "AffiliateLinks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClickEvents_RetailerListings_RetailerListingId",
                        column: x => x.RetailerListingId,
                        principalTable: "RetailerListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateLinks_AffiliateProgramId_Status",
                table: "AffiliateLinks",
                columns: new[] { "AffiliateProgramId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateLinks_RetailerListingId_Status_RevalidateAt",
                table: "AffiliateLinks",
                columns: new[] { "RetailerListingId", "Status", "RevalidateAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AffiliatePrograms_RetailerId_Provider",
                table: "AffiliatePrograms",
                columns: new[] { "RetailerId", "Provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AffiliatePrograms_Status_UpdatedAt",
                table: "AffiliatePrograms",
                columns: new[] { "Status", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClickEvents_AffiliateLinkId_CreatedAt",
                table: "ClickEvents",
                columns: new[] { "AffiliateLinkId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClickEvents_RetailerListingId_CreatedAt",
                table: "ClickEvents",
                columns: new[] { "RetailerListingId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClickEvents");

            migrationBuilder.DropTable(
                name: "AffiliateLinks");

            migrationBuilder.DropTable(
                name: "AffiliatePrograms");
        }
    }
}
