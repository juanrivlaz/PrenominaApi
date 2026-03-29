-- Migración: Crear tabla employee_overtime_configs
-- Fecha: 2026-03-28
-- Descripción: Tabla de configuración para excluir empleados del módulo de horas extras

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'employee_overtime_configs')
BEGIN
    CREATE TABLE employee_overtime_configs (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        employee_code INT NOT NULL,
        company_id INT NOT NULL,
        exclude_overtime BIT NOT NULL DEFAULT 0,
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        updated_at DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );

    CREATE UNIQUE INDEX IX_employee_overtime_configs_employee_company
        ON employee_overtime_configs (employee_code, company_id);
END
GO
