-- ============================================
-- Migration: Add role-based approval chain (cadena de firmas)
-- PrenominaApi Database
-- ============================================
-- Crea:
--   incident_approval_step   -> plantilla: niveles de firma por código de incidencia y empresa
--                               (rol + alcance + orden).
--   absence_request_approval -> instancia "congelada" por solicitud de ausencia: los niveles
--                               materializados con los candidatos resueltos al crear la solicitud.
-- Aditivo y no destructivo: los códigos sin cadena conservan el flujo actual.
-- ============================================

USE [PrenominaApi];
GO

-- ---------- Plantilla: niveles de la cadena ----------
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'incident_approval_step')
BEGIN
    CREATE TABLE incident_approval_step (
        id            UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        incident_code NVARCHAR(450)    NOT NULL,
        step_order    INT              NOT NULL,
        role_id       UNIQUEIDENTIFIER NOT NULL,
        scope         INT              NOT NULL DEFAULT 2,   -- 1=Department, 2=Company
        mode          INT              NOT NULL DEFAULT 1,   -- 1=AnyOne, 2=All
        is_optional   BIT              NOT NULL DEFAULT 0,
        created_at    DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        updated_at    DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        deleted_at    DATETIME2        NULL
    );

    CREATE INDEX IX_incident_approval_step_incident_code ON incident_approval_step (incident_code);

    PRINT 'Created table: incident_approval_step';
END
ELSE
    PRINT 'Table incident_approval_step already exists';
GO

-- ---------- Instancia: cadena congelada por solicitud ----------
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

-- ============================================
-- SEED (ejemplo) — ajustar role_id reales de la tabla [roles] y el company_id.
-- ============================================
-- Permiso (PERM):  Jefe de departamento (Department) -> RH -> Contralor -> Director (Company)
-- Horas extra pagadas (HExtra): Jefe de departamento (Department) -> RH (Company)
--
-- DECLARE @rJefe  UNIQUEIDENTIFIER = (SELECT id FROM roles WHERE label = N'Jefe de departamento');
-- DECLARE @rRH    UNIQUEIDENTIFIER = (SELECT id FROM roles WHERE label = N'Gerencia de RH');
-- DECLARE @rContr UNIQUEIDENTIFIER = (SELECT id FROM roles WHERE label = N'Contralor');
-- DECLARE @rDir   UNIQUEIDENTIFIER = (SELECT id FROM roles WHERE label = N'Director General');
--
-- INSERT INTO incident_approval_step (incident_code, step_order, role_id, scope, mode, is_optional) VALUES
--   ('PERM', 1, @rJefe,  1, 1, 0),
--   ('PERM', 2, @rRH,    2, 1, 0),
--   ('PERM', 3, @rContr, 2, 1, 0),
--   ('PERM', 4, @rDir,   2, 1, 0),
--   ('HExtra', 1, @rJefe, 1, 1, 0),
--   ('HExtra', 2, @rRH,   2, 1, 0);
-- GO
