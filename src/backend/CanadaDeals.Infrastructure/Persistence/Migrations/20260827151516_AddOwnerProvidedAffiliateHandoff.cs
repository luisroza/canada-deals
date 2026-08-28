using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanadaDeals.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerProvidedAffiliateHandoff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AcquisitionMode",
                table: "AffiliateLinks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HandoffMode",
                table: "AffiliateLinks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_AffiliateLinks_OwnerProvidedDirectHandoff",
                table: "AffiliateLinks",
                sql: "(\"AcquisitionMode\" = 0 AND \"HandoffMode\" = 0) OR (\"AcquisitionMode\" = 1 AND \"HandoffMode\" = 1 AND \"Provider\" = 3)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AffiliateLinks_OwnerProvidedDirectHandoff",
                table: "AffiliateLinks");

            migrationBuilder.DropColumn(
                name: "AcquisitionMode",
                table: "AffiliateLinks");

            migrationBuilder.DropColumn(
                name: "HandoffMode",
                table: "AffiliateLinks");
        }
    }
}
