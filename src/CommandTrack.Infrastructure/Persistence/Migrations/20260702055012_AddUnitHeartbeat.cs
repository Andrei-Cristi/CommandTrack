using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommandTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitHeartbeat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSeenAtUtc",
                table: "OperationalUnits",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSeenAtUtc",
                table: "OperationalUnits");
        }
    }
}
