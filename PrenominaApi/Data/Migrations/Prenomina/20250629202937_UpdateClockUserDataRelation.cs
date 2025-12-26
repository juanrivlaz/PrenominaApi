using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrenominaApi.Data.Migrations.Prenomina
{
    /// <inheritdoc />
    public partial class UpdateClockUserDataRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "enroll_number",
                table: "clock_user_finger",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_clock_user_enroll_number",
                table: "clock_user",
                column: "enroll_number");

            migrationBuilder.CreateIndex(
                name: "IX_clock_user_finger_enroll_number",
                table: "clock_user_finger",
                column: "enroll_number");

            migrationBuilder.AddForeignKey(
                name: "FK_clock_user_finger_clock_user_enroll_number",
                table: "clock_user_finger",
                column: "enroll_number",
                principalTable: "clock_user",
                principalColumn: "enroll_number",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_clock_user_finger_clock_user_enroll_number",
                table: "clock_user_finger");

            migrationBuilder.DropIndex(
                name: "IX_clock_user_finger_enroll_number",
                table: "clock_user_finger");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_clock_user_enroll_number",
                table: "clock_user");

            migrationBuilder.AlterColumn<string>(
                name: "enroll_number",
                table: "clock_user_finger",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
