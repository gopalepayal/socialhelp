using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace social_help_and_donation_management_system.Migrations
{
    /// <inheritdoc />
    public partial class AddPickupDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // We only add the columns for the Pickup system, as the tables themselves already exist in the target database.
            // This fix addresses the out-of-sync state between the EF Migrations folder and the real Supabase schema.

            migrationBuilder.AddColumn<bool>(
                name: "IsPickupRequested",
                table: "Donations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PickupStatus",
                table: "Donations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PickupDate",
                table: "Donations",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPickupRequested",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "PickupStatus",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "PickupDate",
                table: "Donations");
        }
    }
}
