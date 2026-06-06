-- ============================================
-- Migration: Add overtime usage for time-off (permisos)
-- PrenominaApi Database
-- ============================================
-- Agrega la columna applied_incident_id a overtime_movement_logs para relacionar
-- el consumo de horas acumuladas con la incidencia/permiso (assistance_incident),
-- permitiendo reintegrar las horas al rechazar/eliminar el permiso y saber en qué
-- días se utilizaron.
-- ============================================

USE [PrenominaApi];
GO

IF NOT EXISTS (
    SELECT * FROM sys.columns
    WHERE Name = N'applied_incident_id'
      AND Object_ID = Object_ID(N'overtime_movement_logs')
)
BEGIN
    ALTER TABLE overtime_movement_logs
        ADD applied_incident_id UNIQUEIDENTIFIER NULL;

    PRINT 'Added column: overtime_movement_logs.applied_incident_id';
END
ELSE
BEGIN
    PRINT 'Column overtime_movement_logs.applied_incident_id already exists';
END
GO

-- Índice para acelerar la búsqueda de consumos por permiso (reintegro/cancelación)
IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE name = N'IX_overtime_movement_logs_applied_incident_id'
      AND object_id = Object_ID(N'overtime_movement_logs')
)
BEGIN
    CREATE INDEX IX_overtime_movement_logs_applied_incident_id
        ON overtime_movement_logs (applied_incident_id);

    PRINT 'Created index: IX_overtime_movement_logs_applied_incident_id';
END
GO
