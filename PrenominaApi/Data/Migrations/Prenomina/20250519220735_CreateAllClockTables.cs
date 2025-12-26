using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrenominaApi.Data.Migrations.Prenomina
{
    /// <inheritdoc />
    public partial class CreateAllClockTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clock",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ip = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    port = table.Column<int>(type: "int", nullable: true),
                    label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clock", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clock_attendace",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    enroll_number = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    verify_mode = table.Column<int>(type: "int", nullable: false),
                    in_out_mode = table.Column<int>(type: "int", nullable: false),
                    year = table.Column<int>(type: "int", nullable: false),
                    month = table.Column<int>(type: "int", nullable: false),
                    day = table.Column<int>(type: "int", nullable: false),
                    hour = table.Column<int>(type: "int", nullable: false),
                    minute = table.Column<int>(type: "int", nullable: false),
                    second = table.Column<int>(type: "int", nullable: false),
                    work_code = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clock_attendace", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clock_user",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    enroll_number = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    privilege = table.Column<int>(type: "int", nullable: false),
                    password = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    enabled = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clock_user", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clock_user_finger",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    enroll_number = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    finger_index = table.Column<int>(type: "int", nullable: false),
                    flag = table.Column<int>(type: "int", nullable: false),
                    finger_base_64 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    finger_length = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clock_user_finger", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_clock_ip",
                table: "clock",
                column: "ip",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_clock_user_enroll_number",
                table: "clock_user",
                column: "enroll_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clock");

            migrationBuilder.DropTable(
                name: "clock_attendace");

            migrationBuilder.DropTable(
                name: "clock_user");

            migrationBuilder.DropTable(
                name: "clock_user_finger");
        }
    }
}
