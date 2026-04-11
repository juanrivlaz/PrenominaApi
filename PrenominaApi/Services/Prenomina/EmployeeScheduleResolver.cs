using Microsoft.EntityFrameworkCore;
using PrenominaApi.Data;
using PrenominaApi.Models;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Repositories;

namespace PrenominaApi.Services.Prenomina
{
    /// <summary>
    /// Centraliza la lógica de "qué horario aplica a un empleado en una fecha dada".
    /// Prioridad: asignación individual del empleado > configuración por actividad > null.
    /// </summary>
    public class EmployeeScheduleResolver
    {
        private readonly PrenominaDbContext _context;
        private readonly IBaseRepository<Key> _keyRepository;

        public EmployeeScheduleResolver(
            PrenominaDbContext context,
            IBaseRepository<Key> keyRepository)
        {
            _context = context;
            _keyRepository = keyRepository;
        }

        /// <summary>
        /// Devuelve el horario activo para un empleado en una fecha específica.
        /// </summary>
        public WorkSchedule? GetScheduleForEmployee(int employeeCode, int companyId, DateOnly onDate)
        {
            // 1. Asignación individual activa para esa fecha
            var employeeAssignment = _context.employeeWorkScheduleAssignments
                .AsNoTracking()
                .Include(a => a.WorkSchedule)
                .Where(a => a.EmployeeCode == employeeCode &&
                            a.CompanyId == companyId &&
                            a.EffectiveFrom <= onDate &&
                            (a.EffectiveTo == null || a.EffectiveTo >= onDate))
                .OrderByDescending(a => a.EffectiveFrom)
                .FirstOrDefault();

            if (employeeAssignment?.WorkSchedule != null)
            {
                return employeeAssignment.WorkSchedule;
            }

            // 2. Configuración por actividad del empleado
            var key = _keyRepository.GetContextEntity()
                .AsNoTracking()
                .Where(k => (int)k.Codigo == employeeCode && k.Company == companyId)
                .Select(k => new { k.Ocupation })
                .FirstOrDefault();

            if (key == null)
            {
                return null;
            }

            var activityConfig = _context.activityWorkScheduleConfigs
                .AsNoTracking()
                .Include(c => c.WorkSchedule)
                .FirstOrDefault(c => c.ActivityId == key.Ocupation && c.CompanyId == companyId);

            return activityConfig?.WorkSchedule;
        }

        /// <summary>
        /// Pre-carga horarios activos para un lote de empleados en un rango de fechas.
        /// Devuelve el horario activo al inicio del rango (o el primero que cubra el rango).
        /// </summary>
        public Dictionary<int, WorkSchedule?> GetSchedulesForEmployees(
            IEnumerable<int> employeeCodes,
            int companyId,
            DateOnly from,
            DateOnly to)
        {
            var codes = employeeCodes.ToList();
            var result = new Dictionary<int, WorkSchedule?>(codes.Count);

            if (codes.Count == 0)
            {
                return result;
            }

            // 1. Asignaciones individuales que se solapan con el rango
            var employeeAssignments = _context.employeeWorkScheduleAssignments
                .AsNoTracking()
                .Include(a => a.WorkSchedule)
                .Where(a => codes.Contains(a.EmployeeCode) &&
                            a.CompanyId == companyId &&
                            a.EffectiveFrom <= to &&
                            (a.EffectiveTo == null || a.EffectiveTo >= from))
                .OrderByDescending(a => a.EffectiveFrom)
                .ToList()
                .GroupBy(a => a.EmployeeCode)
                .ToDictionary(g => g.Key, g => g.First());

            // 2. Recuperar Ocupation por empleado para fallback por actividad
            var keyByCode = _keyRepository.GetContextEntity()
                .AsNoTracking()
                .Where(k => codes.Contains((int)k.Codigo) && k.Company == companyId)
                .Select(k => new { Code = (int)k.Codigo, k.Ocupation })
                .ToList()
                .GroupBy(k => k.Code)
                .ToDictionary(g => g.Key, g => g.First().Ocupation);

            // 3. Configs por actividad para los Ocupation involucrados
            var activityIds = keyByCode.Values.Distinct().ToList();
            var activityConfigs = _context.activityWorkScheduleConfigs
                .AsNoTracking()
                .Include(c => c.WorkSchedule)
                .Where(c => activityIds.Contains(c.ActivityId) && c.CompanyId == companyId)
                .ToList()
                .ToDictionary(c => c.ActivityId, c => c.WorkSchedule);

            foreach (var code in codes)
            {
                if (employeeAssignments.TryGetValue(code, out var assignment) && assignment.WorkSchedule != null)
                {
                    result[code] = assignment.WorkSchedule;
                    continue;
                }

                if (keyByCode.TryGetValue(code, out var ocupation) &&
                    activityConfigs.TryGetValue(ocupation, out var schedule))
                {
                    result[code] = schedule;
                    continue;
                }

                result[code] = null;
            }

            return result;
        }
    }
}
