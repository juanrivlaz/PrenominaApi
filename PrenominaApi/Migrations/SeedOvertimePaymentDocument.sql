-- ============================================
-- Seed: Documento de ejemplo "Solicitud de pago de horas extras"
-- PrenominaApi Database
-- ============================================
-- Inserta en la tabla [document] una plantilla (module = 4 = OvertimePayment) que sirve como
-- formato de la papeleta de pago de horas extras, usando placeholders {{...}}.
-- key_params se guarda como JSON (igual que el resto del módulo de documentos).
-- ============================================

USE [PrenominaApi];
GO

IF NOT EXISTS (
    SELECT 1 FROM document WHERE module = 4 AND deleted_at IS NULL
)
BEGIN
    INSERT INTO document (id, name, path, content, module, key_params, created_at, updated_at, deleted_at)
    VALUES (
        NEWID(),
        N'Solicitud de pago de horas extras (ejemplo)',
        NULL,
        N'<div style="font-family: Helvetica, Arial, sans-serif; color:#1f2430;">
  <div style="text-align:center; margin-bottom:8px;">{{logo}}</div>
  <h2 style="text-align:center; margin:0;">{{companyName}}</h2>
  <h3 style="text-align:center; margin:4px 0 20px; text-transform:uppercase;">Solicitud de pago de horas extras</h3>

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

  <p style="margin:16px 0 8px;">Por medio del presente documento se solicita el pago de las horas extras laboradas:</p>
  <p style="margin:0 0 12px;"><strong>Total de horas extras a pagar: {{totalOvertime}}</strong></p>

  <p style="margin:0 0 4px;">Fechas en que se laboraron las horas extras:</p>
  <p style="margin:0 0 16px;"><strong>{{overtimeDates}}</strong></p>

  <p style="margin:0 0 4px;">OBSERVACIONES / NOTAS:</p>
  <p style="margin:0 0 16px;"><strong>{{notes}}</strong></p>

  {{signatures}}
</div>',
        4, -- DocumentModule.OvertimePayment
        N'["logo","companyName","today","employeeName","employeeActivity","employeeCode","departmentName","totalOvertime","overtimeDates","notes","signatures"]',
        SYSUTCDATETIME(),
        SYSUTCDATETIME(),
        NULL
    );

    PRINT 'Seeded document: Solicitud de pago de horas extras (ejemplo)';
END
ELSE
    PRINT 'Document already exists for module OvertimePayment (4)';
GO
