-- ============================================
-- Seed: Documento de ejemplo "Permiso para ausentarse del trabajo"
-- PrenominaApi Database
-- ============================================
-- Inserta en la tabla [document] una plantilla (module = 2 = Permits) que replica el
-- formato actual del PDF de permiso, usando placeholders {{...}}.
-- key_params se guarda como JSON (igual que el resto del módulo de documentos).
-- ============================================

USE [PrenominaApi];
GO

IF NOT EXISTS (
    SELECT 1 FROM document WHERE name = N'Permiso para ausentarse del trabajo (ejemplo)' AND deleted_at IS NULL
)
BEGIN
    INSERT INTO document (id, name, path, content, module, key_params, created_at, updated_at, deleted_at)
    VALUES (
        NEWID(),
        N'Permiso para ausentarse del trabajo (ejemplo)',
        NULL,
        N'<div style="font-family: Helvetica, Arial, sans-serif; color:#1f2430;">
  <div style="text-align:center; margin-bottom:8px;">{{logo}}</div>
  <h2 style="text-align:center; margin:0;">{{companyName}}</h2>
  <h3 style="text-align:center; margin:4px 0 20px; text-transform:uppercase;">Permiso para ausentarse del trabajo</h3>

  <p style="text-align:right; margin:0 0 16px;">Fecha: <strong>{{today}}</strong></p>

  <table style="width:100%; border-collapse:collapse; margin-bottom:12px;">
    <tr>
      <td style="padding:4px 0;">Nombre: <strong>{{employeeName}}</strong></td>
      <td style="padding:4px 0;">Puesto: <strong>{{employeeActivity}}</strong></td>
    </tr>
    <tr>
      <td style="padding:4px 0;">Código: <strong>{{employeeCode}}</strong></td>
      <td style="padding:4px 0;">Departamento: <strong>{{departmentName}}</strong></td>
    </tr>
  </table>

  <p style="margin:16px 0 8px;">Por medio del presente documento solicito el siguiente permiso:</p>
  <p style="margin:0 0 12px;"><strong>{{permissionLabel}}</strong></p>
  <p style="margin:0 0 16px;"><strong>Días de ausencia que solicita: {{totalDays}}</strong></p>

  <table style="width:100%; border-collapse:collapse; margin-bottom:20px;">
    <tr>
      <td style="padding:4px 0;">Fecha Inicio: <strong>{{startDate}}</strong></td>
      <td style="padding:4px 0;">Fecha Termino: <strong>{{endDate}}</strong></td>
      <td style="padding:4px 0;">Fecha Regreso: <strong>{{returnDate}}</strong></td>
    </tr>
  </table>

  <p style="margin:0 0 4px;">MOTIVOS / OBSERVACIONES / RAZONES:</p>
  <p style="margin:0 0 16px;"><strong>{{notes}}</strong></p>

  {{signatures}}
</div>',
        2, -- DocumentModule.Permits
        N'["logo","companyName","today","employeeName","employeeActivity","employeeCode","departmentName","permissionLabel","totalDays","startDate","endDate","returnDate","notes","signatures"]',
        SYSUTCDATETIME(),
        SYSUTCDATETIME(),
        NULL
    );

    PRINT 'Seeded document: Permiso para ausentarse del trabajo (ejemplo)';
END
ELSE
    PRINT 'Document already exists: Permiso para ausentarse del trabajo (ejemplo)';
GO
