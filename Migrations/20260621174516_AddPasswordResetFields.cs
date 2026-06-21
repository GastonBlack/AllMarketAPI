using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AllMarketAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordResetCodeHash",
                table: "Users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetExpiresAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordResetCodeHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetExpiresAt",
                table: "Users");
        }
    }
}
