using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace social_help_and_donation_management_system.Migrations
{
    /// <inheritdoc />
    public partial class AddPickupLocationField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PickupLocation",
                table: "Donations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PickupLocation",
                table: "Donations");
        }
    }
}
