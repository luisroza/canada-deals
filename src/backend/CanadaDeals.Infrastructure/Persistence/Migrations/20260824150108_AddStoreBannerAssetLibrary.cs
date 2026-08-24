using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanadaDeals.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreBannerAssetLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoreBannerAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreBannerAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreBannerAssets_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreBannerAssets_CreatedAt",
                table: "StoreBannerAssets",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StoreBannerAssets_UploadedByUserId",
                table: "StoreBannerAssets",
                column: "UploadedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreBannerAssets");
        }
    }
}
