using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrenominaApi.Data.Migrations.Prenomina
{
    /// <inheritdoc />
    public partial class CreateIgnoreIncidentToActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ignore_incident_to_activity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    incident_code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    activity_id = table.Column<int>(type: "int", nullable: false),
                    ignore = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ignore_incident_to_activity", x => x.id);
                    table.ForeignKey(
                        name: "FK_ignore_incident_to_activity_incident_code_incident_code",
                        column: x => x.incident_code,
                        principalTable: "incident_code",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ignore_incident_to_activity_incident_code",
                table: "ignore_incident_to_activity",
                column: "incident_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ignore_incident_to_activity");
        }
    }
}
