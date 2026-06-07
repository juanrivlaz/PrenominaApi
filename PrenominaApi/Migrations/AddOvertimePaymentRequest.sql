-- ============================================
-- Migration: Overtime payment request (papeleta de pago de horas extras) - Fase 1
-- PrenominaApi Database
-- ============================================
-- 1) overtime_payment_request -> solicitud/papeleta de pago de horas extras.
-- 2) overtime_movement_logs.overtime_payment_request_id -> vincula los movimientos
--    (DirectPayment) a la papeleta, para poder reintegrar al rechazar.
-- ============================================

USE [PrenominaApi];
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'overtime_payment_request')
BEGIN
    CREATE TABLE overtime_payment_request (
        id                  UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        employee_code       INT              NOT NULL,
        company_id          INT              NOT NULL,
        total_minutes       INT              NOT NULL,
        document_id         UNIQUEIDENTIFIER NULL,
        status              INT              NOT NULL DEFAULT 0,  -- mismo enum AbsenceRequestStatus
        notes               NVARCHAR(500)    NULL,
        created_by_user_id  UNIQUEIDENTIFIER NULL,
        created_at          DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        updated_at          DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        deleted_at          DATETIME2        NULL
    );

    CREATE INDEX IX_overtime_payment_request_employee_code ON overtime_payment_request (employee_code);
    CREATE INDEX IX_overtime_payment_request_company_id    ON overtime_payment_request (company_id);

    PRINT 'Created table: overtime_payment_request';
END
ELSE
    PRINT 'Table overtime_payment_request already exists';
GO

IF NOT EXISTS (
    SELECT * FROM sys.columns
    WHERE Name = N'overtime_payment_request_id' AND Object_ID = Object_ID(N'overtime_movement_logs')
)
BEGIN
    ALTER TABLE overtime_movement_logs ADD overtime_payment_request_id UNIQUEIDENTIFIER NULL;
    PRINT 'Added column: overtime_movement_logs.overtime_payment_request_id';
END
ELSE
    PRINT 'Column overtime_movement_logs.overtime_payment_request_id already exists';
GO
