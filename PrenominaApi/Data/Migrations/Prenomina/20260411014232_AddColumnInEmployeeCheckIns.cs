using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrenominaApi.Data.Migrations.Prenomina
{
    /// <inheritdoc />
    public partial class AddColumnInEmployeeCheckIns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "source_check_in",
                table: "employee_check_ins",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.CreateTable(
                name: "activity_work_schedule_configs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    activity_id = table.Column<int>(type: "int", nullable: false),
                    company_id = table.Column<int>(type: "int", nullable: false),
                    work_schedule_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activity_work_schedule_configs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_activity_work_schedule_configs_work_schedule_work_schedule_id",
                        column: x => x.work_schedule_id,
                        principalTable: "work_schedule",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employee_work_schedule_assignment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    employee_code = table.Column<int>(type: "int", nullable: false),
                    company_id = table.Column<int>(type: "int", nullable: false),
                    work_schedule_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_work_schedule_assignment", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_work_schedule_assignment_work_schedule_work_schedule_id",
                        column: x => x.work_schedule_id,
                        principalTable: "work_schedule",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_activity_work_schedule_configs_activity_id_company_id",
                table: "activity_work_schedule_configs",
                columns: new[] { "activity_id", "company_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_activity_work_schedule_configs_work_schedule_id",
                table: "activity_work_schedule_configs",
                column: "work_schedule_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_work_schedule_assignment_employee_code_company_id_effective_from",
                table: "employee_work_schedule_assignment",
                columns: new[] { "employee_code", "company_id", "effective_from" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_work_schedule_assignment_work_schedule_id",
                table: "employee_work_schedule_assignment",
                column: "work_schedule_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "activity_work_schedule_configs");

            migrationBuilder.DropTable(
                name: "employee_work_schedule_assignment");

            migrationBuilder.DropColumn(
                name: "source_check_in",
                table: "employee_check_ins");
        }
    }
}
