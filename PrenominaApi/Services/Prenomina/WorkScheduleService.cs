using Microsoft.EntityFrameworkCore;
using PrenominaApi.Data;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Models.Prenomina;

namespace PrenominaApi.Services.Prenomina
{
    public class WorkScheduleService
    {
        private readonly PrenominaDbContext _context;

        public WorkScheduleService(PrenominaDbContext context)
        {
            _context = context;
        }

        public List<WorkScheduleOutput> List(int companyId)
        {
            return _context.workSchedules
                .AsNoTracking()
                .Where(w => w.Company == companyId)
                .OrderBy(w => w.Label)
                .Select(w => new WorkScheduleOutput
                {
                    Id = w.Id,
                    Label = w.Label,
                    StartTime = w.StartTime,
                    EndTime = w.EndTime,
                    BreakStart = w.BreakStart,
                    BreakEnd = w.BreakEnd,
                    WorkHours = w.WorkHours,
                    IsNightShift = w.IsNightShift
                })
                .ToList();
        }

        public WorkScheduleOutput? GetById(Guid id)
        {
            var w = _context.workSchedules.AsNoTracking().FirstOrDefault(x => x.Id == id);
            if (w == null) return null;

            return new WorkScheduleOutput
            {
                Id = w.Id,
                Label = w.Label,
                StartTime = w.StartTime,
                EndTime = w.EndTime,
                BreakStart = w.BreakStart,
                BreakEnd = w.BreakEnd,
                WorkHours = w.WorkHours,
                IsNightShift = w.IsNightShift
            };
        }

        public WorkScheduleOutput Create(WorkScheduleInput dto, int companyId)
        {
            var entity = new WorkSchedule
            {
                Company = companyId,
                Label = dto.Label,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                BreakStart = dto.BreakStart,
                BreakEnd = dto.BreakEnd,
                WorkHours = dto.WorkHours,
                IsNightShift = dto.IsNightShift
            };

            _context.workSchedules.Add(entity);
            _context.SaveChanges();

            return new WorkScheduleOutput
            {
                Id = entity.Id,
                Label = entity.Label,
                StartTime = entity.StartTime,
                EndTime = entity.EndTime,
                BreakStart = entity.BreakStart,
                BreakEnd = entity.BreakEnd,
                WorkHours = entity.WorkHours,
                IsNightShift = entity.IsNightShift
            };
        }

        public bool Update(Guid id, WorkScheduleInput dto)
        {
            var entity = _context.workSchedules.FirstOrDefault(w => w.Id == id);
            if (entity == null) return false;

            entity.Label = dto.Label;
            entity.StartTime = dto.StartTime;
            entity.EndTime = dto.EndTime;
            entity.BreakStart = dto.BreakStart;
            entity.BreakEnd = dto.BreakEnd;
            entity.WorkHours = dto.WorkHours;
            entity.IsNightShift = dto.IsNightShift;
            entity.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();
            return true;
        }

        public bool Delete(Guid id)
        {
            var entity = _context.workSchedules.FirstOrDefault(w => w.Id == id);
            if (entity == null) return false;

            // Comprobar si el horario está en uso
            var inUseByEmployee = _context.employeeWorkScheduleAssignments
                .Any(a => a.WorkScheduleId == id && a.EffectiveTo == null);
            var inUseByActivity = _context.activityWorkScheduleConfigs
                .Any(c => c.WorkScheduleId == id);

            if (inUseByEmployee || inUseByActivity)
            {
                throw new BadHttpRequestException("El horario está asignado a empleados o actividades. Desasígnelo primero.");
            }

            entity.DeletedAt = DateTime.UtcNow;
            _context.SaveChanges();
            return true;
        }

        public List<int> GetEmployeesAssignedToSchedule(Guid scheduleId, int companyId)
        {
            return _context.employeeWorkScheduleAssignments
                .AsNoTracking()
                .Where(a => a.WorkScheduleId == scheduleId &&
                            a.CompanyId == companyId &&
                            a.EffectiveTo == null)
                .Select(a => a.EmployeeCode)
                .ToList();
        }

        public bool AssignEmployeeSchedule(int employeeCode, int companyId, Guid? workScheduleId, DateOnly effectiveFrom)
        {
            var active = _context.employeeWorkScheduleAssignments
                .Where(a => a.EmployeeCode == employeeCode &&
                            a.CompanyId == companyId &&
                            a.EffectiveTo == null)
                .ToList();

            // Cerrar las asignaciones activas previas
            foreach (var prev in active)
            {
                if (workScheduleId.HasValue && prev.WorkScheduleId == workScheduleId.Value)
                {
                    // Misma asignación, no hacer nada
                    return true;
                }

                prev.EffectiveTo = effectiveFrom.AddDays(-1);
                prev.UpdatedAt = DateTime.UtcNow;
            }

            if (workScheduleId.HasValue)
            {
                _context.employeeWorkScheduleAssignments.Add(new EmployeeWorkScheduleAssignment
                {
                    EmployeeCode = employeeCode,
                    CompanyId = companyId,
                    WorkScheduleId = workScheduleId.Value,
                    EffectiveFrom = effectiveFrom
                });
            }

            _context.SaveChanges();
            return true;
        }

        public bool AssignBatchEmployeeSchedule(int[] employeeCodes, int companyId, Guid workScheduleId, DateOnly effectiveFrom)
        {
            foreach (var code in employeeCodes)
            {
                AssignEmployeeSchedule(code, companyId, workScheduleId, effectiveFrom);
            }
            return true;
        }

        public bool AssignActivitySchedule(int activityId, int companyId, Guid? workScheduleId)
        {
            var existing = _context.activityWorkScheduleConfigs
                .FirstOrDefault(c => c.ActivityId == activityId && c.CompanyId == companyId);

            if (workScheduleId == null)
            {
                if (existing != null)
                {
                    _context.activityWorkScheduleConfigs.Remove(existing);
                    _context.SaveChanges();
                }
                return true;
            }

            if (existing == null)
            {
                _context.activityWorkScheduleConfigs.Add(new ActivityWorkScheduleConfig
                {
                    ActivityId = activityId,
                    CompanyId = companyId,
                    WorkScheduleId = workScheduleId.Value
                });
            }
            else
            {
                existing.WorkScheduleId = workScheduleId.Value;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            _context.SaveChanges();
            return true;
        }

        public List<EmployeeScheduleAssignmentOutput> GetEmployeeAssignmentHistory(int employeeCode, int companyId)
        {
            return _context.employeeWorkScheduleAssignments
                .AsNoTracking()
                .Include(a => a.WorkSchedule)
                .Where(a => a.EmployeeCode == employeeCode && a.CompanyId == companyId)
                .OrderByDescending(a => a.EffectiveFrom)
                .Select(a => new EmployeeScheduleAssignmentOutput
                {
                    Id = a.Id,
                    EmployeeCode = a.EmployeeCode,
                    WorkScheduleId = a.WorkScheduleId,
                    ScheduleLabel = a.WorkSchedule!.Label,
                    StartTime = a.WorkSchedule.StartTime,
                    EndTime = a.WorkSchedule.EndTime,
                    IsNightShift = a.WorkSchedule.IsNightShift,
                    EffectiveFrom = a.EffectiveFrom,
                    EffectiveTo = a.EffectiveTo
                })
                .ToList();
        }

        public Dictionary<int, EmployeeScheduleAssignmentOutput> GetActiveEmployeeAssignments(int companyId)
        {
            return _context.employeeWorkScheduleAssignments
                .AsNoTracking()
                .Include(a => a.WorkSchedule)
                .Where(a => a.CompanyId == companyId && a.EffectiveTo == null)
                .Select(a => new EmployeeScheduleAssignmentOutput
                {
                    Id = a.Id,
                    EmployeeCode = a.EmployeeCode,
                    WorkScheduleId = a.WorkScheduleId,
                    ScheduleLabel = a.WorkSchedule!.Label,
                    StartTime = a.WorkSchedule.StartTime,
                    EndTime = a.WorkSchedule.EndTime,
                    IsNightShift = a.WorkSchedule.IsNightShift,
                    EffectiveFrom = a.EffectiveFrom,
                    EffectiveTo = a.EffectiveTo
                })
                .ToList()
                .GroupBy(a => a.EmployeeCode)
                .ToDictionary(g => g.Key, g => g.First());
        }

        public Dictionary<int, ActivityScheduleConfigOutput> GetActivityConfigs(int companyId)
        {
            return _context.activityWorkScheduleConfigs
                .AsNoTracking()
                .Include(c => c.WorkSchedule)
                .Where(c => c.CompanyId == companyId)
                .Select(c => new ActivityScheduleConfigOutput
                {
                    ActivityId = c.ActivityId,
                    WorkScheduleId = c.WorkScheduleId,
                    ScheduleLabel = c.WorkSchedule!.Label,
                    IsNightShift = c.WorkSchedule.IsNightShift
                })
                .ToList()
                .ToDictionary(c => c.ActivityId, c => c);
        }
    }
}
