namespace PrenominaApi.Services.Prenomina.Helpers
{
    /// <summary>
    /// Reparte las horas extra autorizadas de un empleado entre los conceptos de nómina
    /// 11, 12 y 13, aplicando reglas diarias y de acumulación semanal.
    ///
    /// Las reglas conviven y SIEMPRE gana el concepto más alto (13 &gt; 12 &gt; 11):
    ///
    ///   Reglas por DÍA (sin importar la semana):
    ///     • horas 1 a 3          → concepto 11
    ///     • hora 4               → concepto 12
    ///     • hora 5 en adelante   → concepto 13
    ///
    ///   Reglas por SEMANA (acumulado de la semana):
    ///     • a partir de la hora 10 acumulada en la semana → concepto 13
    ///     • a partir del 4° día con horas extra en la semana → TODO ese día va a concepto 13
    ///
    /// Las semanas se derivan del periodo de nómina: bloques de 7 días contados desde
    /// <c>periodStart</c> (StartDate del Period). Un día queda en la semana
    /// <c>(fecha - periodStart) / 7</c>. La acumulación se reinicia en cada semana.
    ///
    /// Validado contra los dos ejemplos del requerimiento:
    ///   Ej.1: 4h(3·11 + 1·12), 2h(11), 3h(11), 3h(13 por ser 4° día / hora 10+).
    ///   Ej.2: 1h(11), 1h(11), 1h(11), 3h(13 por ser 4° día con horas extra).
    /// </summary>
    public static class OvertimePaymentConceptCalculator
    {
        public const int Concept11 = 11;
        public const int Concept12 = 12;
        public const int Concept13 = 13;

        // Umbrales en minutos (1 hora = 60 min).
        private const int DailyConcept11LimitMin = 3 * 60;      // 180 → hasta aquí concepto 11 en el día
        private const int DailyConcept12LimitMin = 4 * 60;      // 240 → hasta aquí concepto 12 en el día (hora 4)
        private const int WeeklyConcept13ThresholdMin = 9 * 60; // 540 → el minuto 541 es la "hora 10" de la semana
        private const int OvertimeDayConcept13Index = 4;        // 4° día con horas extra en la semana

        public sealed class DayInput
        {
            public DateOnly Date { get; init; }
            public int OvertimeMinutes { get; init; }
        }

        /// <summary>
        /// Minutos de un día asignados a un concepto concreto. Un mismo día puede generar
        /// hasta 3 segmentos (uno por concepto 11/12/13).
        /// </summary>
        public sealed class ConceptDaySegment
        {
            public DateOnly Date { get; init; }
            public int Concept { get; init; }
            public int Minutes { get; init; }
        }

        /// <summary>
        /// Devuelve el desglose por día y concepto de los días autorizados de un empleado
        /// dentro de un periodo. Ordenado por fecha y luego por concepto.
        /// </summary>
        public static List<ConceptDaySegment> Calculate(
            IEnumerable<DayInput> authorizedDays,
            DateOnly periodStart)
        {
            var segments = new List<ConceptDaySegment>();

            // Agrupar por semana (bloques de 7 días desde el inicio del periodo).
            var weeks = authorizedDays
                .Where(d => d.OvertimeMinutes > 0)
                .GroupBy(d => (d.Date.DayNumber - periodStart.DayNumber) / 7)
                .OrderBy(g => g.Key);

            foreach (var week in weeks)
            {
                var weeklyAccumMin = 0; // minutos acumulados en la semana ANTES del día actual
                var overtimeDay = 0;    // n° de día con horas extra dentro de la semana

                foreach (var day in week.OrderBy(d => d.Date))
                {
                    overtimeDay++;
                    var dayMinutes = day.OvertimeMinutes;

                    // Minutos por concepto SOLO de este día.
                    var perConcept = new Dictionary<int, int>
                    {
                        [Concept11] = 0,
                        [Concept12] = 0,
                        [Concept13] = 0,
                    };

                    if (overtimeDay >= OvertimeDayConcept13Index)
                    {
                        // 4° día (o más) con horas extra en la semana → todo el día a concepto 13.
                        perConcept[Concept13] = dayMinutes;
                    }
                    else
                    {
                        // Resolver minuto a minuto: cada minuto toma el concepto más alto
                        // que le apliquen las reglas diarias y la acumulación semanal.
                        for (var i = 1; i <= dayMinutes; i++)
                        {
                            var dailyPos = i;                    // posición dentro del día
                            var weeklyPos = weeklyAccumMin + i;  // posición acumulada en la semana

                            int concept;
                            if (dailyPos > DailyConcept12LimitMin || weeklyPos > WeeklyConcept13ThresholdMin)
                            {
                                concept = Concept13; // hora 5+ del día, u hora 10+ de la semana
                            }
                            else if (dailyPos > DailyConcept11LimitMin)
                            {
                                concept = Concept12; // hora 4 del día
                            }
                            else
                            {
                                concept = Concept11; // horas 1 a 3 del día
                            }

                            perConcept[concept]++;
                        }
                    }

                    foreach (var concept in new[] { Concept11, Concept12, Concept13 })
                    {
                        if (perConcept[concept] > 0)
                        {
                            segments.Add(new ConceptDaySegment
                            {
                                Date = day.Date,
                                Concept = concept,
                                Minutes = perConcept[concept],
                            });
                        }
                    }

                    weeklyAccumMin += dayMinutes;
                }
            }

            return segments;
        }
    }
}
