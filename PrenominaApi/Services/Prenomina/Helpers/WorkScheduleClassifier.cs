using PrenominaApi.Models.Prenomina;
using PrenominaApi.Models.Prenomina.Enums;

namespace PrenominaApi.Services.Prenomina.Helpers
{
    public static class WorkScheduleClassifier
    {
        public static EntryOrExit ClassifyBySchedule(TimeOnly checkTime, WorkSchedule schedule)
        {
            if (!schedule.IsNightShift)
            {
                var midPoint = schedule.StartTime.AddMinutes(
                    (schedule.EndTime.ToTimeSpan() - schedule.StartTime.ToTimeSpan()).TotalMinutes / 2);
                return checkTime < midPoint ? EntryOrExit.Entry : EntryOrExit.Exit;
            }

            // Turno nocturno: end_time es de madrugada (ej: 06:00), start_time de tarde/noche (ej: 22:00).
            // Checadas antes de end_time + 4h de gracia pertenecen a la salida del turno anterior.
            var cutoff = schedule.EndTime.AddHours(4);
            return checkTime < cutoff ? EntryOrExit.Exit : EntryOrExit.Entry;
        }
    }
}
