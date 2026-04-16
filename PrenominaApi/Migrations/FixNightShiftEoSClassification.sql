-- =============================================================================
-- Fix: Reclasificar EoS (Entry/Exit) para empleados con turno nocturno
-- =============================================================================
-- Problema: La lógica de importación de checadas clasifica como Exit cualquier
-- segundo registro del mismo día. Para turnos nocturnos, un empleado puede tener
-- dos checadas el mismo día:
--   - Salida del turno anterior (ej: 00:43 → debería ser Exit)
--   - Entrada del nuevo turno (ej: 17:16 → debería ser Entry)
-- Ambas caen el mismo día, pero la segunda se clasifica erróneamente como Exit.
--
-- Solución: Usar el horario asignado (work_schedule) para reclasificar basándose
-- en la cercanía de la checada al start_time (Entry) o end_time (Exit).
-- =============================================================================

BEGIN TRANSACTION;

-- CTE: Obtener empleados con turno nocturno y su horario vigente
WITH NightShiftEmployees AS (
    SELECT
        a.employee_code,
        a.company_id,
        ws.start_time,
        ws.end_time,
        a.effective_from,
        a.effective_to
    FROM employee_work_schedule_assignment a
    INNER JOIN work_schedule ws ON ws.id = a.work_schedule_id
    WHERE ws.is_night_shift = 1
      AND ws.deleted_at IS NULL
      AND a.deleted_at IS NULL
),

-- Calcular el EoS correcto para cada checada de empleados nocturnos
CheckInsToFix AS (
    SELECT
        ci.id,
        ci.employee_code,
        ci.company_id,
        ci.date,
        ci.check_in,
        ci.EoS AS current_eos,
        ns.start_time,
        ns.end_time,
        -- Si la checada está más cerca del start_time → Entry (0)
        -- Si la checada está más cerca del end_time → Exit (1)
        -- Para turno nocturno (ej: start 22:00, end 06:00):
        --   - Una checada a las 21:50 está cerca de start → Entry
        --   - Una checada a las 06:10 está cerca de end → Exit
        --   - Una checada a las 17:00 está cerca de start → Entry (nuevo turno)
        CASE
            -- Checada cae en rango cercano al end_time (salida): antes del mediodía
            -- Para turnos nocturnos, la salida siempre es en la madrugada/mañana
            WHEN ci.check_in < DATEADD(HOUR, 4, ns.end_time) THEN 1  -- Exit
            -- Checada cae en rango cercano al start_time (entrada): tarde/noche
            ELSE 0  -- Entry
        END AS correct_eos
    FROM employee_check_ins ci
    INNER JOIN NightShiftEmployees ns
        ON ci.employee_code = ns.employee_code
        AND ci.company_id = ns.company_id
        AND ci.date >= ns.effective_from
        AND (ns.effective_to IS NULL OR ci.date <= ns.effective_to)
    WHERE ci.deleted_at IS NULL
)

-- Primero: ver qué se va a cambiar (quitar comentario del UPDATE para ejecutar)
-- SELECT * FROM CheckInsToFix WHERE current_eos <> correct_eos ORDER BY employee_code, date, check_in;

UPDATE ci
SET
    ci.EoS = ctf.correct_eos,
    ci.updated_at = GETUTCDATE()
FROM employee_check_ins ci
INNER JOIN CheckInsToFix ctf ON ci.id = ctf.id
WHERE ctf.current_eos <> ctf.correct_eos;

PRINT 'Registros actualizados: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

-- Verificar resultados: mostrar checadas corregidas agrupadas por empleado y día
SELECT
    ci.employee_code,
    ci.date,
    ci.check_in,
    CASE ci.EoS WHEN 0 THEN 'Entry' WHEN 1 THEN 'Exit' END AS eos_label,
    ws.label AS schedule_label,
    ws.start_time,
    ws.end_time
FROM employee_check_ins ci
INNER JOIN employee_work_schedule_assignment a
    ON ci.employee_code = a.employee_code
    AND ci.company_id = a.company_id
    AND ci.date >= a.effective_from
    AND (a.effective_to IS NULL OR ci.date <= a.effective_to)
INNER JOIN work_schedule ws ON ws.id = a.work_schedule_id
WHERE ws.is_night_shift = 1
  AND ws.deleted_at IS NULL
  AND a.deleted_at IS NULL
  AND ci.deleted_at IS NULL
ORDER BY ci.employee_code, ci.date, ci.check_in;

COMMIT TRANSACTION;
