using PrenominaApi.Models.Prenomina.Enums;

namespace PrenominaApi.Models.Dto.Input
{
    public class DownloadAttendanceEmployee : GetAttendanceEmployees
    {
        public TypeFileDownload TypeFileDownload { get; set; }
    }
}
