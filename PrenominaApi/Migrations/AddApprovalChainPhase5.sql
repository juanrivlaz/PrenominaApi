-- ============================================
-- Migration: Approval chain phase 5 (modo All + suplencias)
-- PrenominaApi Database
-- ============================================
-- 1) absence_request_approval.signed_user_ids -> firmantes de un nivel (modo All).
-- 2) approver_delegation -> suplencias: un suplente firma por el titular en un rango.
-- ============================================

USE [PrenominaApi];
GO

-- ---------- signed_user_ids ----------
IF NOT EXISTS (
    SELECT * FROM sys.columns
    WHERE Name = N'signed_user_ids' AND Object_ID = Object_ID(N'absence_request_approval')
)
BEGIN
    ALTER TABLE absence_request_approval ADD signed_user_ids NVARCHAR(MAX) NULL;
    PRINT 'Added column: absence_request_approval.signed_user_ids';
END
ELSE
    PRINT 'Column absence_request_approval.signed_user_ids already exists';
GO

-- ---------- approver_delegation ----------
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'approver_delegation')
BEGIN
    CREATE TABLE approver_delegation (
        id               UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        user_id          UNIQUEIDENTIFIER NOT NULL,   -- titular
        delegate_user_id UNIQUEIDENTIFIER NOT NULL,   -- suplente
        from_date        DATE             NOT NULL,
        to_date          DATE             NULL,        -- null = indefinido
        created_at       DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        updated_at       DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        deleted_at       DATETIME2        NULL
    );

    CREATE INDEX IX_approver_delegation_delegate_user_id ON approver_delegation (delegate_user_id);
    CREATE INDEX IX_approver_delegation_user_id          ON approver_delegation (user_id);

    PRINT 'Created table: approver_delegation';
END
ELSE
    PRINT 'Table approver_delegation already exists';
GO
