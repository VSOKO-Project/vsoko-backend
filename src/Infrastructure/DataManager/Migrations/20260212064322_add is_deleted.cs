using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addis_deleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Workloads",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Teachers",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Students",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "StudentGroups",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Refreshes",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Feedbacks",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Employees",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "EmployeeRoles",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Disciplines",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Criterias",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "criteriaFeedbacks",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IsDeleted", table: "Workloads");

            migrationBuilder.DropColumn(name: "IsDeleted", table: "Teachers");

            migrationBuilder.DropColumn(name: "IsDeleted", table: "Students");

            migrationBuilder.DropColumn(name: "IsDeleted", table: "StudentGroups");

            migrationBuilder.DropColumn(name: "IsDeleted", table: "Refreshes");

            migrationBuilder.DropColumn(name: "IsDeleted", table: "Feedbacks");

            migrationBuilder.DropColumn(name: "IsDeleted", table: "Employees");

            migrationBuilder.DropColumn(name: "IsDeleted", table: "EmployeeRoles");

            migrationBuilder.DropColumn(name: "IsDeleted", table: "Disciplines");

            migrationBuilder.DropColumn(name: "IsDeleted", table: "Criterias");

            migrationBuilder.DropColumn(name: "IsDeleted", table: "criteriaFeedbacks");
        }
    }
}
