using PrenominaApi.Models.Dto.Input.Attendance;
using PrenominaApi.Models.Prenomina.Enums;

namespace PrenominaApi.Models.Dto.Output.Attendance
{
    public class DownloadAdditionalPay : GetAdditionalPay
    {
        public TypeFileDownload TypeFileDownload { get; set; }
    }
}
