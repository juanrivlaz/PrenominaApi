using PrenominaApi.Models.Prenomina.Enums;

namespace PrenominaApi.Models.Dto
{
    public class GlobalPropertyService
    {
        public int YearOfOperation { get; set; }
        public TypeTenant TypeTenant { get; set; }
        public string? UserId { get; set; }

        /// <summary>
        /// Centro/supervisor seleccionado (header "tenant"). "-999" = TODOS (sin filtro por centro).
        /// </summary>
        public string Tenant { get; set; } = "-999";
    }
}
