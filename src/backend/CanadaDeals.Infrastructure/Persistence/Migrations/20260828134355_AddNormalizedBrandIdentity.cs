using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanadaDeals.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNormalizedBrandIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedKey",
                table: "Brands",
                type: "character varying(140)",
                maxLength: 140,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE "Brands"
                SET "NormalizedKey" = trim(regexp_replace(
                    lower(replace(replace(replace("Name", '®', ''), '™', ''), '©', '')),
                    '[^[:alnum:]]+', ' ', 'g'));

                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM "Brands"
                        GROUP BY "NormalizedKey"
                        HAVING count(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Duplicate normalized brand identities must be reconciled before this migration can continue.';
                    END IF;
                END $$;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Brands_NormalizedKey",
                table: "Brands",
                column: "NormalizedKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Brands_NormalizedKey",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "NormalizedKey",
                table: "Brands");
        }
    }
}
