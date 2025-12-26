using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrenominaApi.Data.Migrations.Prenomina
{
    /// <inheritdoc />
    public partial class AddRehiredEmployees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rehired_employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    employee_code = table.Column<int>(type: "int", nullable: false),
                    company_id = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    contract_folio = table.Column<int>(type: "int", nullable: false),
                    apply_rehired = table.Column<bool>(type: "bit", nullable: false),
                    observation = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rehired_employees", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rehired_employees_company_id",
                table: "rehired_employees",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_rehired_employees_employee_code",
                table: "rehired_employees",
                column: "employee_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rehired_employees");
        }
    }
}
