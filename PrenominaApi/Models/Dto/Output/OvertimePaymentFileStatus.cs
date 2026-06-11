namespace PrenominaApi.Models.Dto.Output
{
    /// <summary>
    /// Estado de generación del archivo de tiempo extra de un periodo. Sirve como indicador
    /// para evitar pagar dos veces (el archivo se sube a una aplicación externa).
    /// </summary>
    public class OvertimePaymentFileStatus
    {
        /// <summary>True si el archivo del periodo ya fue generado/descargado al menos una vez.</summary>
        public bool Generated { get; set; }

        /// <summary>Fecha/hora de la primera generación.</summary>
        public DateTime? GeneratedAt { get; set; }

        /// <summary>Fecha/hora de la última generación (si se regeneró).</summary>
        public DateTime? LastGeneratedAt { get; set; }

        /// <summary>Número de veces que se ha generado.</summary>
        public int GenerationCount { get; set; }

        /// <summary>Nombre del usuario que lo generó por última vez.</summary>
        public string? GeneratedByName { get; set; }

        /// <summary>Renglones incluidos en la última generación.</summary>
        public int LineCount { get; set; }

        /// <summary>Importe total de la última generación.</summary>
        public decimal TotalAmount { get; set; }
    }
}
