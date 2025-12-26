using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrenominaApi.Data.Migrations.Prenomina
{
    /// <inheritdoc />
    public partial class AddCustomPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_section_rol",
                table: "section_rol");

            migrationBuilder.DropIndex(
                name: "IX_section_rol_SectionsCode",
                table: "section_rol");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "section_rol",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "section_rol",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "permissions_json",
                table: "section_rol",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "section_rol",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddPrimaryKey(
                name: "PK_section_rol",
                table: "section_rol",
                columns: new[] { "SectionsCode", "RolesId" });

            migrationBuilder.CreateIndex(
                name: "IX_section_rol_RolesId",
                table: "section_rol",
                column: "RolesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_section_rol",
                table: "section_rol");

            migrationBuilder.DropIndex(
                name: "IX_section_rol_RolesId",
                table: "section_rol");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "section_rol");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "section_rol");

            migrationBuilder.DropColumn(
                name: "permissions_json",
                table: "section_rol");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "section_rol");

            migrationBuilder.AddPrimaryKey(
                name: "PK_section_rol",
                table: "section_rol",
                columns: new[] { "RolesId", "SectionsCode" });

            migrationBuilder.CreateIndex(
                name: "IX_section_rol_SectionsCode",
                table: "section_rol",
                column: "SectionsCode");
        }
    }
}
