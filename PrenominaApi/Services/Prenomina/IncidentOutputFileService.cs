using ClosedXML.Excel;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Repositories.Prenomina;

namespace PrenominaApi.Services.Prenomina
{
    public class IncidentOutputFileService : ServicePrenomina<IncidentOutputFile>
    {
        private readonly IBaseRepositoryPrenomina<ColumnIncidentOutputFile> _columnIncidentFileRepository;
        public IncidentOutputFileService(
            IBaseRepositoryPrenomina<IncidentOutputFile> baseRepository,
            IBaseRepositoryPrenomina<ColumnIncidentOutputFile> columnIncidentFileRepository
        ) : base(baseRepository) {
            _columnIncidentFileRepository = columnIncidentFileRepository;
        }

        public IncidentOutputFile ExecuteProcess(CreateIncidentOutputFile incidentOutputFile)
        {
            var existFile = _repository.GetByFilter(f => f.Name.ToLower() == incidentOutputFile.Name.ToLower()).FirstOrDefault();

            if (existFile != null) {
                throw new BadHttpRequestException("El nombre del archivo ya se encuentra registrado");
            }

            var createFile = _repository.Create(new IncidentOutputFile { Name = incidentOutputFile.Name });

            foreach (var item in incidentOutputFile.Columns)
            {
                _columnIncidentFileRepository.Create(new ColumnIncidentOutputFile
                {
                    Name = item.Name,
                    CustomValue = item.CustomValue,
                    KeyValueId = item.KeyValueId,
                });
            }

            _columnIncidentFileRepository.Save();
            _repository.Save();

            return createFile;
        }

        public byte[] ExecuteProcess(string incidentCode)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Datos");
                worksheet.Cell("A1").Value = "Nombre";
                worksheet.Cell("B1").Value = "Edad";
                worksheet.Cell("A2").Value = "Juan";
                worksheet.Cell("B2").Value = 30;
                worksheet.Cell("A3").Value = "Ana";
                worksheet.Cell("B3").Value = 25;

                // Guardar en memoria
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);

                    return stream.ToArray();
                }
            }
        }
    }
}
