using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.DataManager.Migrations
{
    /// <inheritdoc />
    public partial class AddAcademicPeriods : Migration
    {
        private const string InitialPeriodId = "5b0e7c1e-2026-4a17-9c3e-000000000001";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcademicPeriods",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    StartYear = table.Column<int>(type: "integer", nullable: false),
                    Term = table.Column<int>(type: "integer", nullable: false),
                    IsFeedbackOpen = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "NOW()"),
                    CreatedById = table.Column<string>(type: "text", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedById = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicPeriods", x => x.Id);
                });

            // Существующие данные попадают в период 2026/27, осенний семестр, сбор открыт.
            migrationBuilder.Sql($"""
                INSERT INTO "AcademicPeriods" ("Id", "StartYear", "Term", "IsFeedbackOpen", "IsDeleted", "CreatedAtUtc")
                VALUES ('{InitialPeriodId}', 2026, 1, true, false, NOW());
                """);

            migrationBuilder.AddColumn<string>(
                name: "PeriodId",
                table: "Workloads",
                type: "text",
                nullable: true);

            migrationBuilder.Sql($"""UPDATE "Workloads" SET "PeriodId" = '{InitialPeriodId}';""");

            migrationBuilder.AlterColumn<string>(
                name: "PeriodId",
                table: "Workloads",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_Workloads_TeacherId",
                table: "Workloads");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_StudentId",
                table: "Feedbacks");

            migrationBuilder.CreateIndex(
                name: "IX_Workloads_PeriodId",
                table: "Workloads",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_Workloads_TeacherId_DisciplineId_GroupId_PeriodId",
                table: "Workloads",
                columns: new[] { "TeacherId", "DisciplineId", "GroupId", "PeriodId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_Surname_Name_Patronymic",
                table: "Teachers",
                columns: new[] { "Surname", "Name", "Patronymic" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_StudentId_WorkloadId",
                table: "Feedbacks",
                columns: new[] { "StudentId", "WorkloadId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Disciplines_Name",
                table: "Disciplines",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicPeriods_IsFeedbackOpen",
                table: "AcademicPeriods",
                column: "IsFeedbackOpen",
                unique: true,
                filter: "\"IsFeedbackOpen\" = true AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicPeriods_StartYear_Term",
                table: "AcademicPeriods",
                columns: new[] { "StartYear", "Term" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddForeignKey(
                name: "FK_Workloads_AcademicPeriods_PeriodId",
                table: "Workloads",
                column: "PeriodId",
                principalTable: "AcademicPeriods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Workloads_AcademicPeriods_PeriodId",
                table: "Workloads");

            migrationBuilder.DropTable(
                name: "AcademicPeriods");

            migrationBuilder.DropIndex(
                name: "IX_Workloads_PeriodId",
                table: "Workloads");

            migrationBuilder.DropIndex(
                name: "IX_Workloads_TeacherId_DisciplineId_GroupId_PeriodId",
                table: "Workloads");

            migrationBuilder.DropIndex(
                name: "IX_Teachers_Surname_Name_Patronymic",
                table: "Teachers");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_StudentId_WorkloadId",
                table: "Feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_Disciplines_Name",
                table: "Disciplines");

            migrationBuilder.DropColumn(
                name: "PeriodId",
                table: "Workloads");

            migrationBuilder.CreateIndex(
                name: "IX_Workloads_TeacherId",
                table: "Workloads",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_StudentId",
                table: "Feedbacks",
                column: "StudentId");
        }
    }
}
