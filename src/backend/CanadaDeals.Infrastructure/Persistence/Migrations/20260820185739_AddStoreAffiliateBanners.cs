using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanadaDeals.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreAffiliateBanners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "Retailers",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "RetailerListingId",
                table: "ClickEvents",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "AffiliateLinkId",
                table: "ClickEvents",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "AffiliateProgramId",
                table: "ClickEvents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RetailerId",
                table: "ClickEvents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StoreAffiliateDestinationId",
                table: "ClickEvents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StoreAffiliateDestinations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RetailerId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_StoreAffiliateDestinations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreAffiliateDestinations_AffiliatePrograms_AffiliateProgr~",
                        column: x => x.AffiliateProgramId,
                        principalTable: "AffiliatePrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoreAffiliateDestinations_Retailers_RetailerId",
                        column: x => x.RetailerId,
                        principalTable: "Retailers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoreBannerProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RetailerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Subtitle = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    AssetPath = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    AssetSource = table.Column<int>(type: "integer", nullable: false),
                    BrandAssetPolicy = table.Column<int>(type: "integer", nullable: false),
                    BannerOrder = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AssetEvidenceReference = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EffectiveAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreBannerProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreBannerProfiles_Retailers_RetailerId",
                        column: x => x.RetailerId,
                        principalTable: "Retailers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClickEvents_AffiliateProgramId",
                table: "ClickEvents",
                column: "AffiliateProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_ClickEvents_RetailerId_CreatedAt",
                table: "ClickEvents",
                columns: new[] { "RetailerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClickEvents_StoreAffiliateDestinationId_CreatedAt",
                table: "ClickEvents",
                columns: new[] { "StoreAffiliateDestinationId", "CreatedAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_ClickEvents_Source",
                table: "ClickEvents",
                sql: "(\"AffiliateLinkId\" IS NOT NULL AND \"RetailerListingId\" IS NOT NULL AND \"StoreAffiliateDestinationId\" IS NULL) OR (\"AffiliateLinkId\" IS NULL AND \"RetailerListingId\" IS NULL AND \"StoreAffiliateDestinationId\" IS NOT NULL AND \"RetailerId\" IS NOT NULL AND \"AffiliateProgramId\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_StoreAffiliateDestinations_AffiliateProgramId",
                table: "StoreAffiliateDestinations",
                column: "AffiliateProgramId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreAffiliateDestinations_RetailerId_Status_RevalidateAt",
                table: "StoreAffiliateDestinations",
                columns: new[] { "RetailerId", "Status", "RevalidateAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StoreBannerProfiles_IsEnabled_BannerOrder",
                table: "StoreBannerProfiles",
                columns: new[] { "IsEnabled", "BannerOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_StoreBannerProfiles_RetailerId",
                table: "StoreBannerProfiles",
                column: "RetailerId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ClickEvents_AffiliatePrograms_AffiliateProgramId",
                table: "ClickEvents",
                column: "AffiliateProgramId",
                principalTable: "AffiliatePrograms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClickEvents_Retailers_RetailerId",
                table: "ClickEvents",
                column: "RetailerId",
                principalTable: "Retailers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClickEvents_StoreAffiliateDestinations_StoreAffiliateDestin~",
                table: "ClickEvents",
                column: "StoreAffiliateDestinationId",
                principalTable: "StoreAffiliateDestinations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClickEvents_AffiliatePrograms_AffiliateProgramId",
                table: "ClickEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_ClickEvents_Retailers_RetailerId",
                table: "ClickEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_ClickEvents_StoreAffiliateDestinations_StoreAffiliateDestin~",
                table: "ClickEvents");

            migrationBuilder.DropTable(
                name: "StoreAffiliateDestinations");

            migrationBuilder.DropTable(
                name: "StoreBannerProfiles");

            migrationBuilder.DropIndex(
                name: "IX_ClickEvents_AffiliateProgramId",
                table: "ClickEvents");

            migrationBuilder.DropIndex(
                name: "IX_ClickEvents_RetailerId_CreatedAt",
                table: "ClickEvents");

            migrationBuilder.DropIndex(
                name: "IX_ClickEvents_StoreAffiliateDestinationId_CreatedAt",
                table: "ClickEvents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ClickEvents_Source",
                table: "ClickEvents");

            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "Retailers");

            migrationBuilder.DropColumn(
                name: "AffiliateProgramId",
                table: "ClickEvents");

            migrationBuilder.DropColumn(
                name: "RetailerId",
                table: "ClickEvents");

            migrationBuilder.DropColumn(
                name: "StoreAffiliateDestinationId",
                table: "ClickEvents");

            migrationBuilder.AlterColumn<Guid>(
                name: "RetailerListingId",
                table: "ClickEvents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "AffiliateLinkId",
                table: "ClickEvents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
