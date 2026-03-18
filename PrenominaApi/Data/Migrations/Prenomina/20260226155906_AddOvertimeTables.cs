using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrenominaApi.Data.Migrations.Prenomina
{
    /// <inheritdoc />
    public partial class AddOvertimeTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "overtime_accumulations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    employee_code = table.Column<int>(type: "int", nullable: false),
                    company_id = table.Column<int>(type: "int", nullable: false),
                    accumulated_minutes = table.Column<int>(type: "int", nullable: false),
                    used_minutes = table.Column<int>(type: "int", nullable: false),
                    paid_minutes = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_overtime_accumulations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "overtime_movement_logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    overtime_accumulation_id = table.Column<int>(type: "int", nullable: false),
                    employee_code = table.Column<int>(type: "int", nullable: false),
                    company_id = table.Column<int>(type: "int", nullable: false),
                    movement_type = table.Column<int>(type: "int", nullable: false),
                    minutes = table.Column<int>(type: "int", nullable: false),
                    balance_after = table.Column<int>(type: "int", nullable: false),
                    source_date = table.Column<DateOnly>(type: "date", nullable: false),
                    applied_rest_date = table.Column<DateOnly>(type: "date", nullable: true),
                    original_check_in = table.Column<TimeOnly>(type: "time", nullable: true),
                    original_check_out = table.Column<TimeOnly>(type: "time", nullable: true),
                    notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    related_movement_id = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_overtime_movement_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_overtime_movement_logs_overtime_accumulations_overtime_accumulation_id",
                        column: x => x.overtime_accumulation_id,
                        principalTable: "overtime_accumulations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_overtime_movement_logs_overtime_movement_logs_related_movement_id",
                        column: x => x.related_movement_id,
                        principalTable: "overtime_movement_logs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_overtime_movement_logs_user_by_user_id",
                        column: x => x.by_user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_overtime_accumulations_employee_code_company_id",
                table: "overtime_accumulations",
                columns: new[] { "employee_code", "company_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_overtime_movement_logs_by_user_id",
                table: "overtime_movement_logs",
                column: "by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_overtime_movement_logs_company_id_created_at",
                table: "overtime_movement_logs",
                columns: new[] { "company_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_overtime_movement_logs_employee_code_source_date",
                table: "overtime_movement_logs",
                columns: new[] { "employee_code", "source_date" });

            migrationBuilder.CreateIndex(
                name: "IX_overtime_movement_logs_overtime_accumulation_id",
                table: "overtime_movement_logs",
                column: "overtime_accumulation_id");

            migrationBuilder.CreateIndex(
                name: "IX_overtime_movement_logs_related_movement_id",
                table: "overtime_movement_logs",
                column: "related_movement_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "overtime_movement_logs");

            migrationBuilder.DropTable(
                name: "overtime_accumulations");
        }
    }
}
