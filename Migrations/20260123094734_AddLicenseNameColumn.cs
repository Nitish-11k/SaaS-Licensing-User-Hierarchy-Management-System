using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaasLicenseSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLicenseNameColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Licenses",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Licenses");
        }
    }
}
