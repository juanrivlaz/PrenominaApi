using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrenominaApi.Data.Migrations.Prenomina
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employee_check_ins",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    employee_code = table.Column<int>(type: "int", nullable: false),
                    check_in = table.Column<TimeOnly>(type: "time", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    num_conc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EoS = table.Column<int>(type: "int", nullable: false),
                    period = table.Column<int>(type: "int", nullable: false),
                    type_nom = table.Column<int>(type: "int", nullable: false),
                    employee_schedule = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_check_ins", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "incident_code_metadata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    math_operation = table.Column<int>(type: "int", nullable: false),
                    column_for_operation = table.Column<int>(type: "int", nullable: false),
                    custom_value = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incident_code_metadata", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "section",
                columns: table => new
                {
                    code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_section", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "system_config",
                columns: table => new
                {
                    key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    data = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_config", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "incident_code",
                columns: table => new
                {
                    code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    external_code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    required_approval = table.Column<bool>(type: "bit", nullable: false),
                    with_operation = table.Column<bool>(type: "bit", nullable: false),
                    is_additional = table.Column<bool>(type: "bit", nullable: false),
                    metadata_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incident_code", x => x.code);
                    table.ForeignKey(
                        name: "FK_incident_code_incident_code_metadata_metadata_id",
                        column: x => x.metadata_id,
                        principalTable: "incident_code_metadata",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    role_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "section_rol",
                columns: table => new
                {
                    RolesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SectionsCode = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_section_rol", x => new { x.RolesId, x.SectionsCode });
                    table.ForeignKey(
                        name: "FK_section_rol_roles_RolesId",
                        column: x => x.RolesId,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_section_rol_section_SectionsCode",
                        column: x => x.SectionsCode,
                        principalTable: "section",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "assistance_incident",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    company_id = table.Column<int>(type: "int", nullable: false),
                    employee_code = table.Column<int>(type: "int", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    incident_code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    time_off_request = table.Column<bool>(type: "bit", nullable: false),
                    approved = table.Column<bool>(type: "bit", nullable: false),
                    by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assistance_incident", x => x.id);
                    table.ForeignKey(
                        name: "FK_assistance_incident_incident_code_incident_code",
                        column: x => x.incident_code,
                        principalTable: "incident_code",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_assistance_incident_user_by_user_id",
                        column: x => x.by_user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    section_code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    record_id = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    old_value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    new_value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_log", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_log_user_by_user_id",
                        column: x => x.by_user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "incident_approver",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    incident_code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incident_approver", x => x.id);
                    table.ForeignKey(
                        name: "FK_incident_approver_incident_code_incident_code",
                        column: x => x.incident_code,
                        principalTable: "incident_code",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_incident_approver_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_company",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    company_id = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_company", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_company_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "assistance_incident_approver",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    assistance_incident_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    incident_approver_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    approval_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assistance_incident_approver", x => x.id);
                    table.ForeignKey(
                        name: "FK_assistance_incident_approver_assistance_incident_assistance_incident_id",
                        column: x => x.assistance_incident_id,
                        principalTable: "assistance_incident",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_assistance_incident_approver_incident_approver_incident_approver_id",
                        column: x => x.incident_approver_id,
                        principalTable: "incident_approver",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_department",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_company_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    department_code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_department", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_department_user_company_user_company_id",
                        column: x => x.user_company_id,
                        principalTable: "user_company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_supervisor",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_company_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supervisor_id = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_supervisor", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_supervisor_user_company_user_company_id",
                        column: x => x.user_company_id,
                        principalTable: "user_company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_assistance_incident_by_user_id",
                table: "assistance_incident",
                column: "by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_assistance_incident_company_id",
                table: "assistance_incident",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_assistance_incident_date",
                table: "assistance_incident",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "IX_assistance_incident_employee_code",
                table: "assistance_incident",
                column: "employee_code");

            migrationBuilder.CreateIndex(
                name: "IX_assistance_incident_incident_code",
                table: "assistance_incident",
                column: "incident_code");

            migrationBuilder.CreateIndex(
                name: "IX_assistance_incident_approver_assistance_incident_id",
                table: "assistance_incident_approver",
                column: "assistance_incident_id");

            migrationBuilder.CreateIndex(
                name: "IX_assistance_incident_approver_incident_approver_id",
                table: "assistance_incident_approver",
                column: "incident_approver_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_by_user_id",
                table: "audit_log",
                column: "by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_check_ins_date",
                table: "employee_check_ins",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "IX_employee_check_ins_employee_code",
                table: "employee_check_ins",
                column: "employee_code");

            migrationBuilder.CreateIndex(
                name: "IX_incident_approver_incident_code",
                table: "incident_approver",
                column: "incident_code");

            migrationBuilder.CreateIndex(
                name: "IX_incident_approver_user_id",
                table: "incident_approver",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_incident_code_code",
                table: "incident_code",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_incident_code_metadata_id",
                table: "incident_code",
                column: "metadata_id",
                unique: true,
                filter: "[metadata_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_section_code",
                table: "section",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_section_rol_SectionsCode",
                table: "section_rol",
                column: "SectionsCode");

            migrationBuilder.CreateIndex(
                name: "IX_system_config_key",
                table: "system_config",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_email",
                table: "user",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_role_id",
                table: "user",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_company_user_id",
                table: "user_company",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_department_user_company_id",
                table: "user_department",
                column: "user_company_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_supervisor_user_company_id",
                table: "user_supervisor",
                column: "user_company_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assistance_incident_approver");

            migrationBuilder.DropTable(
                name: "audit_log");

            migrationBuilder.DropTable(
                name: "employee_check_ins");

            migrationBuilder.DropTable(
                name: "section_rol");

            migrationBuilder.DropTable(
                name: "system_config");

            migrationBuilder.DropTable(
                name: "user_department");

            migrationBuilder.DropTable(
                name: "user_supervisor");

            migrationBuilder.DropTable(
                name: "assistance_incident");

            migrationBuilder.DropTable(
                name: "incident_approver");

            migrationBuilder.DropTable(
                name: "section");

            migrationBuilder.DropTable(
                name: "user_company");

            migrationBuilder.DropTable(
                name: "incident_code");

            migrationBuilder.DropTable(
                name: "user");

            migrationBuilder.DropTable(
                name: "incident_code_metadata");

            migrationBuilder.DropTable(
                name: "roles");
        }
    }
}
