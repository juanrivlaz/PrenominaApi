-- ============================================
-- Migration: Approval chain instance table
-- PrenominaApi Database
-- ============================================
-- absence_request_approval -> instancia "congelada" por solicitud de ausencia: los niveles
-- materializados con los candidatos resueltos al crear la solicitud.
-- (La PLANTILLA de la cadena vive en el documento/contrato: ver AddDocumentApprovalChain.sql)
-- ============================================

USE [PrenominaApi];
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'absence_request_approval')
BEGIN
    CREATE TABLE absence_request_approval (
        id                          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        absence_request_id          UNIQUEIDENTIFIER NOT NULL,
        step_order                  INT              NOT NULL,
        role_id                     UNIQUEIDENTIFIER NOT NULL,
        scope                       INT              NOT NULL DEFAULT 2,
        mode                        INT              NOT NULL DEFAULT 1,
        is_optional                 BIT              NOT NULL DEFAULT 0,
        status                      INT              NOT NULL DEFAULT 0,  -- 0=Pending,1=Approved,2=Rejected,3=Skipped,4=Blocked
        resolved_candidate_user_ids NVARCHAR(MAX)    NULL,               -- CSV de GUIDs (snapshot)
        signed_user_ids             NVARCHAR(MAX)    NULL,               -- CSV de GUIDs (firmantes, modo All)
        approved_by_user_id         UNIQUEIDENTIFIER NULL,
        approved_at                 DATETIME2        NULL,
        comment                     NVARCHAR(500)    NULL,
        created_at                  DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        updated_at                  DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        deleted_at                  DATETIME2        NULL
    );

    CREATE INDEX IX_absence_request_approval_absence_request_id ON absence_request_approval (absence_request_id);

    PRINT 'Created table: absence_request_approval';
END
ELSE
    PRINT 'Table absence_request_approval already exists';
GO
