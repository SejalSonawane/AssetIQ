using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetIQ.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceTicketFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompletionNotes",
                table: "maintenance_records",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "maintenance_records",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "maintenance_records",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "maintenance_records",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletionNotes",
                table: "maintenance_records");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "maintenance_records");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "maintenance_records");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "maintenance_records");
        }
    }
}
