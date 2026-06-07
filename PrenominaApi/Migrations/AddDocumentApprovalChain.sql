-- ============================================
-- Migration: Move approval chain template to the document (contrato)
-- PrenominaApi Database
-- ============================================
-- 1) document_approval_step -> PLANTILLA de la cadena de firmas, dueña el documento/contrato.
-- 2) incident_code.document_id -> documento/contrato asignado al código de incidencia.
-- 3) Elimina la tabla legacy incident_approval_step (la plantilla ya no vive en el código).
-- ============================================

USE [PrenominaApi];
GO

-- ---------- Plantilla en el documento ----------
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'document_approval_step')
BEGIN
    CREATE TABLE document_approval_step (
        id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        document_id UNIQUEIDENTIFIER NOT NULL,
        step_order  INT              NOT NULL,
        role_id     UNIQUEIDENTIFIER NOT NULL,
        scope       INT              NOT NULL DEFAULT 2,   -- 1=Department, 2=Company
        mode        INT              NOT NULL DEFAULT 1,   -- 1=AnyOne, 2=All
        is_optional BIT              NOT NULL DEFAULT 0,
        created_at  DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        updated_at  DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        deleted_at  DATETIME2        NULL
    );

    CREATE INDEX IX_document_approval_step_document_id ON document_approval_step (document_id);

    PRINT 'Created table: document_approval_step';
END
ELSE
    PRINT 'Table document_approval_step already exists';
GO

-- ---------- Documento asignado al código de incidencia ----------
IF NOT EXISTS (
    SELECT * FROM sys.columns
    WHERE Name = N'document_id' AND Object_ID = Object_ID(N'incident_code')
)
BEGIN
    ALTER TABLE incident_code ADD document_id UNIQUEIDENTIFIER NULL;
    PRINT 'Added column: incident_code.document_id';
END
ELSE
    PRINT 'Column incident_code.document_id already exists';
GO

-- ---------- Limpieza de la tabla legacy ----------
IF EXISTS (SELECT * FROM sys.tables WHERE name = N'incident_approval_step')
BEGIN
    DROP TABLE incident_approval_step;
    PRINT 'Dropped legacy table: incident_approval_step';
END
GO
