using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RevitToIfcScheduler.Migrations
{
    /// <inheritdoc />
    public partial class AddConversionJobFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FolderUrl",
                table: "ConversionJobs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FolderUrl",
                table: "ConversionJobs");
        }
    }
}
