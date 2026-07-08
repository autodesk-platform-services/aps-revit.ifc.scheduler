using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RevitToIfcScheduler.Migrations
{
    /// <inheritdoc />
    public partial class AddHashedSessionKeyIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "HashedSessionKey",
                table: "Users",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_HashedSessionKey",
                table: "Users",
                column: "HashedSessionKey",
                unique: true,
                filter: "[HashedSessionKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_HashedSessionKey",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "HashedSessionKey",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);
        }
    }
}
