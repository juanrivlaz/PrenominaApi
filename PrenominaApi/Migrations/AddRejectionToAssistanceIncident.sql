-- Agrega campos de rechazo a assistance_incident
ALTER TABLE assistance_incident
ADD rejected BIT NOT NULL DEFAULT 0,
    rejection_comment NVARCHAR(500) NULL,
    rejected_by_user_id UNIQUEIDENTIFIER NULL,
    rejected_at DATETIME2 NULL;
