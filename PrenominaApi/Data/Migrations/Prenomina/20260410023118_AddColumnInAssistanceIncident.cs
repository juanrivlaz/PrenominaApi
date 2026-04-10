using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrenominaApi.Data.Migrations.Prenomina
{
    /// <inheritdoc />
    public partial class AddColumnInAssistanceIncident : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "rejected",
                table: "assistance_incident",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "rejected_at",
                table: "assistance_incident",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "rejected_by_user_id",
                table: "assistance_incident",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rejection_comment",
                table: "assistance_incident",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "rejected",
                table: "assistance_incident");

            migrationBuilder.DropColumn(
                name: "rejected_at",
                table: "assistance_incident");

            migrationBuilder.DropColumn(
                name: "rejected_by_user_id",
                table: "assistance_incident");

            migrationBuilder.DropColumn(
                name: "rejection_comment",
                table: "assistance_incident");
        }
    }
}
