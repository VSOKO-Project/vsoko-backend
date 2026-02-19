using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixRefreshForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Refreshes_AspNetUsers_Id",
                table: "Refreshes");

            migrationBuilder.CreateIndex(
                name: "IX_Refreshes_UserId",
                table: "Refreshes",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Refreshes_AspNetUsers_UserId",
                table: "Refreshes",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Refreshes_AspNetUsers_UserId",
                table: "Refreshes");

            migrationBuilder.DropIndex(
                name: "IX_Refreshes_UserId",
                table: "Refreshes");

            migrationBuilder.AddForeignKey(
                name: "FK_Refreshes_AspNetUsers_Id",
                table: "Refreshes",
                column: "Id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
