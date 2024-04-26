using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RevitToIfcScheduler.Migrations
{
    /// <inheritdoc />
    public partial class RemoveConversionJobFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ForgeUrl",
                table: "ConversionJobs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ForgeUrl",
                table: "ConversionJobs",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
