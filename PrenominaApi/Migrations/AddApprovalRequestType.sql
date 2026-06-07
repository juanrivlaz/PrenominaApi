-- ============================================
-- Migration: Generalizar la cadena de firmas (request_type)
-- PrenominaApi Database
-- ============================================
-- Permite que absence_request_approval sirva tanto a permisos como a pagos de horas extras.
-- request_type: 1 = AbsenceRequest (permiso), 2 = OvertimePayment (pago HE).
-- absence_request_id pasa a ser el id genérico de la solicitud dueña.
-- ============================================

USE [PrenominaApi];
GO

IF NOT EXISTS (
    SELECT * FROM sys.columns
    WHERE Name = N'request_type' AND Object_ID = Object_ID(N'absence_request_approval')
)
BEGIN
    ALTER TABLE absence_request_approval ADD request_type INT NOT NULL DEFAULT 1;
    PRINT 'Added column: absence_request_approval.request_type';
END
ELSE
    PRINT 'Column absence_request_approval.request_type already exists';
GO
