using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanadaDeals.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductImagePublishing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceListingId = table.Column<Guid>(type: "uuid", nullable: true),
                    MerchantPolicyId = table.Column<Guid>(type: "uuid", nullable: true),
                    Origin = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Provider = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    RightsEvidenceReference = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    AllowedPlacements = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    EffectiveAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastValidatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductImages_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductImages_MerchantPolicies_MerchantPolicyId",
                        column: x => x.MerchantPolicyId,
                        principalTable: "MerchantPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductImages_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductImages_RetailerListings_SourceListingId",
                        column: x => x.SourceListingId,
                        principalTable: "RetailerListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ContentHash",
                table: "ProductImages",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_MerchantPolicyId",
                table: "ProductImages",
                column: "MerchantPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId_State_CreatedAt",
                table: "ProductImages",
                columns: new[] { "ProductId", "State", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_SourceListingId",
                table: "ProductImages",
                column: "SourceListingId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_UploadedByUserId",
                table: "ProductImages",
                column: "UploadedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductImages");
        }
    }
}
