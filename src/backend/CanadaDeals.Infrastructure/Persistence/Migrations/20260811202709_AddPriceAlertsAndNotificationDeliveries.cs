using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanadaDeals.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceAlertsAndNotificationDeliveries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PriceAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetPrice = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TargetVersion = table.Column<int>(type: "integer", nullable: false),
                    IsBelowTargetCycle = table.Column<bool>(type: "boolean", nullable: false),
                    ConsentGrantedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConsentVersion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastEvaluatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastTriggeredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceAlerts", x => x.Id);
                    table.CheckConstraint("CK_PriceAlerts_TargetPrice", "\"TargetPrice\" > 0 AND \"TargetPrice\" <= 1000000");
                    table.CheckConstraint("CK_PriceAlerts_TargetVersion", "\"TargetVersion\" > 0");
                    table.ForeignKey(
                        name: "FK_PriceAlerts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PriceAlerts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceAlertId = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceObservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetVersion = table.Column<int>(type: "integer", nullable: false),
                    TargetPrice = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    QualifyingPrice = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DestinationAddress = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    StatusReason = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationDeliveries_PriceAlerts_PriceAlertId",
                        column: x => x.PriceAlertId,
                        principalTable: "PriceAlerts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificationDeliveries_PriceObservations_PriceObservationId",
                        column: x => x.PriceObservationId,
                        principalTable: "PriceObservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_PriceAlertId_TargetVersion_PriceObse~",
                table: "NotificationDeliveries",
                columns: new[] { "PriceAlertId", "TargetVersion", "PriceObservationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_PriceObservationId",
                table: "NotificationDeliveries",
                column: "PriceObservationId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_Status_CreatedAt",
                table: "NotificationDeliveries",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceAlerts_ProductId",
                table: "PriceAlerts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceAlerts_Status_LastEvaluatedAt",
                table: "PriceAlerts",
                columns: new[] { "Status", "LastEvaluatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceAlerts_UserId_ProductId",
                table: "PriceAlerts",
                columns: new[] { "UserId", "ProductId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationDeliveries");

            migrationBuilder.DropTable(
                name: "PriceAlerts");
        }
    }
}
