namespace PrenominaApi.Models.Dto.Output
{
    /// <summary>
    /// Renglón del archivo de importación de tiempo extra (NóminaTISS-SAR).
    /// Estructura: CODIGO(8,N) | CONCEPTO(4,N) | IMPORTE(10.2,N) | FECHA(dd/mm/aaaa,C) | HORAS(6.2,N)
    /// </summary>
    public class OvertimePaymentConceptLine
    {
        /// <summary>CODIGO: código de empleado vigente en el periodo.</summary>
        public int EmployeeCode { get; set; }

        /// <summary>Nombre completo del empleado (solo informativo para la previsualización; no se exporta al archivo).</summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>Puesto del empleado (solo informativo para la previsualización; no se exporta al archivo).</summary>
        public string JobPosition { get; set; } = string.Empty;

        /// <summary>CONCEPTO: 11 (horas 1-3), 12 (hora 4), 13 (hora 5+, semana 10+ o 4° día).</summary>
        public int Concept { get; set; }

        /// <summary>IMPORTE: (sueldo/8) * horas * factor (11 y 12 → ×2; 13 → ×3).</summary>
        public decimal Amount { get; set; }

        /// <summary>FECHA del tiempo extra (se exporta como dd/MM/yyyy).</summary>
        public DateOnly Date { get; set; }

        /// <summary>HORAS extra del concepto en ese día (minutos / 60, 2 decimales).</summary>
        public decimal Hours { get; set; }
    }
}
