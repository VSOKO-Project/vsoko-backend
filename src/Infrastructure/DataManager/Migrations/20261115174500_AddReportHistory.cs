using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataManager.Migrations;

public partial class AddReportHistory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ReportArchives",
            columns: table => new
            {
                Id = table.Column<string>(type: "text", nullable: false),
                PeriodTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                FileName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                Content = table.Column<byte[]>(type: "bytea", nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_ReportArchives", x => x.Id));
        migrationBuilder.CreateIndex("IX_ReportArchives_CreatedAtUtc_Id", "ReportArchives", new[] { "CreatedAtUtc", "Id" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("ReportArchives");
}
