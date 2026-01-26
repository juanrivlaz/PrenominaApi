using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrenominaApi.Data.Migrations.Prenomina
{
    /// <inheritdoc />
    public partial class CreateIncidentCodeAllowedRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "incident_code_allowed_roles",
                columns: table => new
                {
                    incident_code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    role_code = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ItemIncidentCodeCode = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incident_code_allowed_roles", x => new { x.incident_code, x.role_code });
                    table.ForeignKey(
                        name: "FK_incident_code_allowed_roles_incident_code_ItemIncidentCodeCode",
                        column: x => x.ItemIncidentCodeCode,
                        principalTable: "incident_code",
                        principalColumn: "code");
                    table.ForeignKey(
                        name: "FK_incident_code_allowed_roles_incident_code_incident_code",
                        column: x => x.incident_code,
                        principalTable: "incident_code",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_incident_code_allowed_roles_roles_role_code",
                        column: x => x.role_code,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_incident_code_allowed_roles_ItemIncidentCodeCode",
                table: "incident_code_allowed_roles",
                column: "ItemIncidentCodeCode");

            migrationBuilder.CreateIndex(
                name: "IX_incident_code_allowed_roles_role_code",
                table: "incident_code_allowed_roles",
                column: "role_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "incident_code_allowed_roles");
        }
    }
}
