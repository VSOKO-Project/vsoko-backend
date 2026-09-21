using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataManager.Migrations
{
    /// <inheritdoc />
    public partial class BindRefreshToSecurityStamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SecurityStamp",
                table: "Refreshes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "Refreshes");
        }
    }
}
