using DocumentFormat.OpenXml.Presentation;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrenominaApi.Data.Migrations.Prenomina
{
    /// <inheritdoc />
    public partial class AddIndexOverTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================
            //Indices para rendimiento
            // ============================================

            //Indice para busqueda por empleado y fecha origen
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OvertimeMovementLogs_Employee_SourceDate' AND object_id = OBJECT_ID('overtime_movement_logs'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_OvertimeMovementLogs_Employee_SourceDate
                    ON overtime_movement_logs (employee_code, source_date)
                    INCLUDE (movement_type, minutes, balance_after);
                END
            ");

            //Indice para busqueda por empresa y fecha de creacion
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OvertimeMovementLogs_Company_CreatedAt' AND object_id = OBJECT_ID('overtime_movement_logs'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_OvertimeMovementLogs_Company_CreatedAt
                    ON overtime_movement_logs (company_id, created_at DESC)
                    INCLUDE (employee_code, movement_type, minutes);
                END
            ");

            //Indice para busqueda por tipo de movimiento
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OvertimeMovementLogs_MovementType' AND object_id = OBJECT_ID('overtime_movement_logs'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_OvertimeMovementLogs_MovementType
                    ON overtime_movement_logs (movement_type)
                    INCLUDE (employee_code, company_id, minutes, source_date);
                END
            ");

            //Indice para busqueda de movimientos relacionados (cancelaciones)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OvertimeMovementLogs_RelatedMovement' AND object_id = OBJECT_ID('overtime_movement_logs'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_OvertimeMovementLogs_RelatedMovement
                    ON overtime_movement_logs (related_movement_id)
                    WHERE related_movement_id IS NOT NULL;
                END
            ");

            // ============================================
            //Actualizar estadisticas
            // ============================================
            migrationBuilder.Sql(@"
                UPDATE STATISTICS overtime_accumulations;
            ");

            migrationBuilder.Sql(@"
                UPDATE STATISTICS overtime_movement_logs;
            ");

            // ===============================
            // assistance_incidents
            // ===============================

            migrationBuilder.CreateIndex(
                name: "IX_AssistanceIncidents_Employee_Date",
                table: "assistance_incident",
                columns: new[] { "employee_code", "date" })
                .Annotation("SqlServer:Include", new[]
                { "company_id", "incident_code", "approved", "time_off_request", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_AssistanceIncidents_Company_Date",
                table: "assistance_incident",
                columns: new[] { "company_id", "date" })
                .Annotation("SqlServer:Include", new[]
                { "employee_code", "incident_code", "approved" });

            migrationBuilder.CreateIndex(
                name: "IX_AssistanceIncidents_IncidentCode",
                table: "assistance_incident",
                column: "incident_code")
                .Annotation("SqlServer:Include", new[]
                { "employee_code", "date", "approved" });

            // ===============================
            // employee_check_ins
            // ===============================

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCheckIns_Employee_Date",
                table: "employee_check_ins",
                columns: new[] { "employee_code", "Date" })
                .Annotation("SqlServer:Include", new[]
                { "company_id", "check_in", "EoS", "type_nom" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCheckIns_Company_Date",
                table: "employee_check_ins",
                columns: new[] { "company_id", "date" })
                .Annotation("SqlServer:Include", new[]
                { "employee_code", "check_in", "EoS" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCheckIns_EoS",
                table: "employee_check_ins",
                columns: new[] { "EoS", "date" })
                .Annotation("SqlServer:Include", new[]
                { "employee_code", "check_in", "company_id" });


            // ===============================
            // Periods
            // ===============================

            migrationBuilder.CreateIndex(
                name: "IX_Periods_Company_Year_Type",
                table: "period",
                columns: new[] { "company", "year", "type_payroll" })
                .Annotation("SqlServer:Include", new[]
                { "num_period", "start_date", "closing_date", "is_active" });

            // ===============================
            // IncidentCodes
            // ===============================

            migrationBuilder.CreateIndex(
                name: "IX_IncidentCodes_IsAdditional",
                table: "incident_code",
                column: "is_additional")
                .Annotation("SqlServer:Include", new[]
                { "code", "label", "with_operation", "restricted_with_roles" });

            // ===============================
            // PeriodStatus
            // ===============================

            migrationBuilder.CreateIndex(
                name: "IX_PeriodStatus_Type_Period_Company",
                table: "period_status",
                columns: new[] { "type_payroll", "num_period", "company" })
                .Annotation("SqlServer:Include", new[]
                { "tenant_id" });

            // ===============================
            // Users
            // ===============================

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "user",
                column: "email")
                .Annotation("SqlServer:Include", new[]
                { "id", "name", "password", "role_id" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "user",
                column: "role_id")
                .Annotation("SqlServer:Include", new[]
                { "id", "email", "name" });

            // ===============================
            // UserCompanies
            // ===============================

            migrationBuilder.CreateIndex(
                name: "IX_UserCompanies_UserId",
                table: "user_company",
                column: "user_id")
                .Annotation("SqlServer:Include", new[]
                { "company_id" });

            // ===============================
            // Update Statistics
            // ===============================

            migrationBuilder.Sql(@"
                UPDATE STATISTICS assistance_incident;
                UPDATE STATISTICS employee_check_ins;
                UPDATE STATISTICS period;
                UPDATE STATISTICS incident_code;
                UPDATE STATISTICS period_status;
                UPDATE STATISTICS [user];
                UPDATE STATISTICS user_company;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 
                    FROM sys.indexes 
                    WHERE name = 'IX_OvertimeMovementLogs_Employee_SourceDate' 
                        AND object_id = OBJECT_ID('overtime_movement_logs')
                )
                BEGIN
                    DROP INDEX IX_OvertimeMovementLogs_Employee_SourceDate 
                    ON overtime_movement_logs;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 
                    FROM sys.indexes 
                    WHERE name = 'IX_OvertimeMovementLogs_Company_CreatedAt' 
                        AND object_id = OBJECT_ID('overtime_movement_logs')
                )
                BEGIN
                    DROP INDEX IX_OvertimeMovementLogs_Company_CreatedAt 
                    ON overtime_movement_logs;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 
                    FROM sys.indexes 
                    WHERE name = 'IX_OvertimeMovementLogs_MovementType' 
                        AND object_id = OBJECT_ID('overtime_movement_logs')
                )
                BEGIN
                    DROP INDEX IX_OvertimeMovementLogs_MovementType 
                    ON overtime_movement_logs;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 
                    FROM sys.indexes 
                    WHERE name = 'IX_OvertimeMovementLogs_RelatedMovement' 
                        AND object_id = OBJECT_ID('overtime_movement_logs')
                )
                BEGIN
                    DROP INDEX IX_OvertimeMovementLogs_RelatedMovement 
                    ON overtime_movement_logs;
                END
            ");

            migrationBuilder.DropIndex("IX_AssistanceIncidents_Employee_Date", "assistance_incident");
            migrationBuilder.DropIndex("IX_AssistanceIncidents_Company_Date", "assistance_incident");
            migrationBuilder.DropIndex("IX_AssistanceIncidents_IncidentCode", "assistance_incident");

            migrationBuilder.DropIndex("IX_EmployeeCheckIns_Employee_Date", "employee_check_ins");
            migrationBuilder.DropIndex("IX_EmployeeCheckIns_Company_Date", "employee_check_ins");
            migrationBuilder.DropIndex("IX_EmployeeCheckIns_EoS", "employee_check_ins");

            migrationBuilder.DropIndex("IX_Periods_Company_Year_Type", "period");

            migrationBuilder.DropIndex("IX_IncidentCodes_IsAdditional", "incident_code");

            migrationBuilder.DropIndex("IX_PeriodStatus_Type_Period_Company", "period_status");

            migrationBuilder.DropIndex("IX_Users_Email", "user");
            migrationBuilder.DropIndex("IX_Users_RoleId", "user");

            migrationBuilder.DropIndex("IX_UserCompanies_UserId", "user_company");
        }
    }
}
