using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanadaDeals.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionEmailDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeliveredAt",
                table: "NotificationDeliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastAttemptedAt",
                table: "NotificationDeliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastProviderEventAt",
                table: "NotificationDeliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProviderAcceptedAt",
                table: "NotificationDeliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderMessageId",
                table: "NotificationDeliveries",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AccountConfirmationDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationAddress = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    ProviderMessageId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    StatusReason = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastAttemptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProviderAcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastProviderEventAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountConfirmationDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountConfirmationDeliveries_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ControlledEmailCaptures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DestinationAddress = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    Subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    HtmlBody = table.Column<string>(type: "text", nullable: false),
                    TextBody = table.Column<string>(type: "text", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlledEmailCaptures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailSuppressions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NormalizedAddress = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    Reason = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailSuppressions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedEmailWebhooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    EventId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    EventType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ProviderMessageId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    ProviderCreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedEmailWebhooks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_ProviderMessageId",
                table: "NotificationDeliveries",
                column: "ProviderMessageId",
                unique: true,
                filter: "\"ProviderMessageId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccountConfirmationDeliveries_ProviderMessageId",
                table: "AccountConfirmationDeliveries",
                column: "ProviderMessageId",
                unique: true,
                filter: "\"ProviderMessageId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccountConfirmationDeliveries_UserId_CreatedAt",
                table: "AccountConfirmationDeliveries",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ControlledEmailCaptures_DestinationAddress_CapturedAt",
                table: "ControlledEmailCaptures",
                columns: new[] { "DestinationAddress", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ControlledEmailCaptures_IdempotencyKey",
                table: "ControlledEmailCaptures",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailSuppressions_NormalizedAddress",
                table: "EmailSuppressions",
                column: "NormalizedAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedEmailWebhooks_Provider_EventId",
                table: "ProcessedEmailWebhooks",
                columns: new[] { "Provider", "EventId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountConfirmationDeliveries");

            migrationBuilder.DropTable(
                name: "ControlledEmailCaptures");

            migrationBuilder.DropTable(
                name: "EmailSuppressions");

            migrationBuilder.DropTable(
                name: "ProcessedEmailWebhooks");

            migrationBuilder.DropIndex(
                name: "IX_NotificationDeliveries_ProviderMessageId",
                table: "NotificationDeliveries");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "NotificationDeliveries");

            migrationBuilder.DropColumn(
                name: "LastAttemptedAt",
                table: "NotificationDeliveries");

            migrationBuilder.DropColumn(
                name: "LastProviderEventAt",
                table: "NotificationDeliveries");

            migrationBuilder.DropColumn(
                name: "ProviderAcceptedAt",
                table: "NotificationDeliveries");

            migrationBuilder.DropColumn(
                name: "ProviderMessageId",
                table: "NotificationDeliveries");
        }
    }
}
