using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrenominaApi.Data.Migrations.Prenomina
{
    /// <inheritdoc />
    public partial class LastFixs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "work_days",
                table: "work_schedule",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_connection_at",
                table: "user",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "path",
                table: "document",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "content",
                table: "document",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "module",
                table: "document",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_sync_at",
                table: "clock",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "employee_clock_blocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    employee_code = table.Column<int>(type: "int", nullable: false),
                    company_id = table.Column<int>(type: "int", nullable: false),
                    is_blocked = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_clock_blocks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_employee_clock_blocks_employee_code_company_id",
                table: "employee_clock_blocks",
                columns: new[] { "employee_code", "company_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_clock_blocks");

            migrationBuilder.DropColumn(
                name: "work_days",
                table: "work_schedule");

            migrationBuilder.DropColumn(
                name: "last_connection_at",
                table: "user");

            migrationBuilder.DropColumn(
                name: "content",
                table: "document");

            migrationBuilder.DropColumn(
                name: "module",
                table: "document");

            migrationBuilder.DropColumn(
                name: "last_sync_at",
                table: "clock");

            migrationBuilder.AlterColumn<string>(
                name: "path",
                table: "document",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
