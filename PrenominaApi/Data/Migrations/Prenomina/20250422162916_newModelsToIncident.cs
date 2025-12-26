using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrenominaApi.Data.Migrations.Prenomina
{
    /// <inheritdoc />
    public partial class newModelsToIncident : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "day_off",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    incident_code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    is_union = table.Column<bool>(type: "bit", nullable: false),
                    is_sunday = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_day_off", x => x.id);
                    table.ForeignKey(
                        name: "FK_day_off_incident_code_incident_code",
                        column: x => x.incident_code,
                        principalTable: "incident_code",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ignore_incident_to_employee",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    incident_code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    employee_code = table.Column<int>(type: "int", nullable: false),
                    ignore = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ignore_incident_to_employee", x => x.id);
                    table.ForeignKey(
                        name: "FK_ignore_incident_to_employee_incident_code_incident_code",
                        column: x => x.incident_code,
                        principalTable: "incident_code",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ignore_incident_to_tenant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    incident_code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    department_code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    supervisor_id = table.Column<int>(type: "int", nullable: true),
                    ignore = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ignore_incident_to_tenant", x => x.id);
                    table.ForeignKey(
                        name: "FK_ignore_incident_to_tenant_incident_code_incident_code",
                        column: x => x.incident_code,
                        principalTable: "incident_code",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "incident_output_file",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incident_output_file", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "key_value",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_key_value", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "column_incident_output_file",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    value = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    custom_value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_column_incident_output_file", x => x.id);
                    table.ForeignKey(
                        name: "FK_column_incident_output_file_key_value_value",
                        column: x => x.value,
                        principalTable: "key_value",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_column_incident_output_file_value",
                table: "column_incident_output_file",
                column: "value");

            migrationBuilder.CreateIndex(
                name: "IX_day_off_date",
                table: "day_off",
                column: "date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_day_off_incident_code",
                table: "day_off",
                column: "incident_code");

            migrationBuilder.CreateIndex(
                name: "IX_ignore_incident_to_employee_incident_code",
                table: "ignore_incident_to_employee",
                column: "incident_code");

            migrationBuilder.CreateIndex(
                name: "IX_ignore_incident_to_tenant_incident_code",
                table: "ignore_incident_to_tenant",
                column: "incident_code");

            migrationBuilder.CreateIndex(
                name: "IX_key_value_code",
                table: "key_value",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "column_incident_output_file");

            migrationBuilder.DropTable(
                name: "day_off");

            migrationBuilder.DropTable(
                name: "ignore_incident_to_employee");

            migrationBuilder.DropTable(
                name: "ignore_incident_to_tenant");

            migrationBuilder.DropTable(
                name: "incident_output_file");

            migrationBuilder.DropTable(
                name: "key_value");
        }
    }
}
