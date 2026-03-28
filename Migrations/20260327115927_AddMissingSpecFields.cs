using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace social_help_and_donation_management_system.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingSpecFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClothCondition",
                table: "Requirements",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressProofFilePath",
                table: "Organisations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdProofFilePath",
                table: "Organisations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClothCondition",
                table: "Donations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClothCondition",
                table: "Requirements");

            migrationBuilder.DropColumn(
                name: "AddressProofFilePath",
                table: "Organisations");

            migrationBuilder.DropColumn(
                name: "IdProofFilePath",
                table: "Organisations");

            migrationBuilder.DropColumn(
                name: "ClothCondition",
                table: "Donations");
        }
    }
}
