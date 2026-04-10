-- Agrega campos de rechazo y agrupamiento a assistance_incident
ALTER TABLE assistance_incident
ADD rejected BIT NOT NULL DEFAULT 0,
    rejection_comment NVARCHAR(500) NULL,
    rejected_by_user_id UNIQUEIDENTIFIER NULL,
    rejected_at DATETIME2 NULL,
    request_group_id UNIQUEIDENTIFIER NULL;

CREATE INDEX IX_assistance_incident_request_group_id
ON assistance_incident (request_group_id)
WHERE request_group_id IS NOT NULL;
