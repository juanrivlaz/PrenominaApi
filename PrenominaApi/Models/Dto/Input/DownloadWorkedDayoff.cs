using PrenominaApi.Models.Prenomina.Enums;

namespace PrenominaApi.Models.Dto.Input
{
    public class DownloadWorkedDayoff : GetWorkedDayOff
    {
        public required TypeFileDownload TypeFileDownload { get; set; }
    }
}
