-- ============================================
-- Migration: Add Overtime Accumulation Tables
-- PrenominaApi Database
-- ============================================

USE [PrenominaApi];
GO

-- ============================================
-- Tabla: overtime_accumulations
-- Almacena el balance acumulado de horas extras por empleado
-- ============================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'overtime_accumulations')
BEGIN
    CREATE TABLE overtime_accumulations (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        employee_code INT NOT NULL,
        company_id INT NOT NULL,
        accumulated_minutes INT NOT NULL DEFAULT 0,
        used_minutes INT NOT NULL DEFAULT 0,
        paid_minutes INT NOT NULL DEFAULT 0,
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        updated_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT UQ_OvertimeAccumulation_Employee_Company UNIQUE (employee_code, company_id)
    );

    PRINT 'Created table: overtime_accumulations';
END
GO

-- ============================================
-- Tabla: overtime_movement_logs
-- Log de todos los movimientos de horas extras para trazabilidad
-- ============================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'overtime_movement_logs')
BEGIN
    CREATE TABLE overtime_movement_logs (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        overtime_accumulation_id INT NOT NULL,
        employee_code INT NOT NULL,
        company_id INT NOT NULL,
        movement_type INT NOT NULL,
        minutes INT NOT NULL,
        balance_after INT NOT NULL,
        source_date DATE NOT NULL,
        applied_rest_date DATE NULL,
        original_check_in TIME NULL,
        original_check_out TIME NULL,
        notes NVARCHAR(500) NULL,
        by_user_id UNIQUEIDENTIFIER NOT NULL,
        related_movement_id INT NULL,
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_OvertimeMovementLog_Accumulation FOREIGN KEY (overtime_accumulation_id)
            REFERENCES overtime_accumulations(Id),
        CONSTRAINT FK_OvertimeMovementLog_User FOREIGN KEY (by_user_id)
            REFERENCES [user](id),
        CONSTRAINT FK_OvertimeMovementLog_RelatedMovement FOREIGN KEY (related_movement_id)
            REFERENCES overtime_movement_logs(Id)
    );

    PRINT 'Created table: overtime_movement_logs';
END
GO

-- ============================================
-- Indices para rendimiento
-- ============================================

-- Indice para busqueda por empleado y fecha origen
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OvertimeMovementLogs_Employee_SourceDate' AND object_id = OBJECT_ID('overtime_movement_logs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OvertimeMovementLogs_Employee_SourceDate
    ON overtime_movement_logs (employee_code, source_date)
    INCLUDE (movement_type, minutes, balance_after);

    PRINT 'Created index: IX_OvertimeMovementLogs_Employee_SourceDate';
END
GO

-- Indice para busqueda por empresa y fecha de creacion
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OvertimeMovementLogs_Company_CreatedAt' AND object_id = OBJECT_ID('overtime_movement_logs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OvertimeMovementLogs_Company_CreatedAt
    ON overtime_movement_logs (company_id, created_at DESC)
    INCLUDE (employee_code, movement_type, minutes);

    PRINT 'Created index: IX_OvertimeMovementLogs_Company_CreatedAt';
END
GO

-- Indice para busqueda por tipo de movimiento
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OvertimeMovementLogs_MovementType' AND object_id = OBJECT_ID('overtime_movement_logs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OvertimeMovementLogs_MovementType
    ON overtime_movement_logs (movement_type)
    INCLUDE (employee_code, company_id, minutes, source_date);

    PRINT 'Created index: IX_OvertimeMovementLogs_MovementType';
END
GO

-- Indice para busqueda de movimientos relacionados (cancelaciones)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OvertimeMovementLogs_RelatedMovement' AND object_id = OBJECT_ID('overtime_movement_logs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OvertimeMovementLogs_RelatedMovement
    ON overtime_movement_logs (related_movement_id)
    WHERE related_movement_id IS NOT NULL;

    PRINT 'Created index: IX_OvertimeMovementLogs_RelatedMovement';
END
GO

-- ============================================
-- Actualizar estadisticas
-- ============================================

UPDATE STATISTICS overtime_accumulations;
UPDATE STATISTICS overtime_movement_logs;

PRINT 'Statistics updated successfully.';
GO

-- ============================================
-- Comentarios de documentacion
-- ============================================

-- Movement Types:
-- 1 = Accumulation (horas acumuladas desde tiempo extra trabajado)
-- 2 = UsedForRestDay (horas usadas para dia de descanso)
-- 3 = DirectPayment (horas pagadas directamente sin acumular)
-- 4 = ManualAdjustment (ajuste manual administrativo)
-- 5 = Cancellation (cancelacion de movimiento previo)

PRINT '============================================';
PRINT 'Overtime Accumulation tables created successfully!';
PRINT '============================================';
