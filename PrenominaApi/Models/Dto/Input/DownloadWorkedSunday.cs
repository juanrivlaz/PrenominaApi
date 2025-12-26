using PrenominaApi.Models.Prenomina.Enums;

namespace PrenominaApi.Models.Dto.Input
{
    public class DownloadWorkedSunday : GetWorkedSunday
    {
        public required TypeFileDownload TypeFileDownload { get; set; }
    }
}
