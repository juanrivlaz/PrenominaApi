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
            // Ventana de salida: desde 1h antes de endTime hasta endTime + 4h de gracia.
            // Ventana de entrada: desde 3h antes de startTime hasta 3h después de startTime.
            // Fuera de ambas ventanas: clasificar por proximidad temporal a cada extremo del turno.
            var exitWindowStart = schedule.EndTime.AddHours(-1);
            var exitWindowEnd = schedule.EndTime.AddHours(4);

            // La ventana de salida no cruza medianoche (endTime es de madrugada, ej: 02:00 a 10:00).
            if (checkTime >= exitWindowStart && checkTime < exitWindowEnd)
            {
                return EntryOrExit.Exit;
            }

            // Ventana de entrada (puede cruzar medianoche si startTime es muy tarde).
            // Caso simple: startTime entre 03:00 y 21:00 → no cruza medianoche.
            // Caso típico nocturno: startTime entre 21:00 y 23:59 → entryStart puede ser 18:00..20:59.
            var entryWindowStart = schedule.StartTime.AddHours(-3);
            var entryWindowEnd = schedule.StartTime.AddHours(3);

            if (entryWindowStart <= entryWindowEnd)
            {
                if (checkTime >= entryWindowStart && checkTime <= entryWindowEnd)
                {
                    return EntryOrExit.Entry;
                }
            }
            else
            {
                // Cruza medianoche: hora >= entryWindowStart O hora <= entryWindowEnd
                if (checkTime >= entryWindowStart || checkTime <= entryWindowEnd)
                {
                    return EntryOrExit.Entry;
                }
            }

            // Fuera de ambas ventanas: usar el cutoff original como fallback conservador.
            var cutoff = schedule.EndTime.AddHours(4);
            return checkTime < cutoff ? EntryOrExit.Exit : EntryOrExit.Entry;
        }
    }
}
