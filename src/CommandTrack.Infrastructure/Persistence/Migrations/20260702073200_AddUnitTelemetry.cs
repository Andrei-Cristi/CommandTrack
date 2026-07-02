using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommandTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitTelemetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UnitTelemetry",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatteryPercent = table.Column<double>(type: "float", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    SpeedKph = table.Column<double>(type: "float", nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitTelemetry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnitTelemetry_OperationalUnits_UnitId",
                        column: x => x.UnitId,
                        principalTable: "OperationalUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnitTelemetry_UnitId_RecordedAtUtc",
                table: "UnitTelemetry",
                columns: new[] { "UnitId", "RecordedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnitTelemetry");
        }
    }
}
