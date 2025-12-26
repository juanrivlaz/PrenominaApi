using Microsoft.EntityFrameworkCore;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Repositories.Prenomina;

namespace PrenominaApi.Services.Prenomina
{
    public class IncidentCodeService : ServicePrenomina<IncidentCode>
    {
        public readonly IBaseRepositoryPrenomina<User> _userRepository;
        public readonly IBaseRepositoryPrenomina<IncidentCodeMetadata> _incidentCodeMetadataRepo;
        public readonly IBaseRepositoryPrenomina<IncidentApprover> _incidentApproverRepo;

        public IncidentCodeService(
            IBaseRepositoryPrenomina<IncidentCode> baseRepository,
            IBaseRepositoryPrenomina<User> userRepository,
            IBaseRepositoryPrenomina<IncidentCodeMetadata> incidentCodeMetadataRepo,
            IBaseRepositoryPrenomina<IncidentApprover> incidentApproverRepo
        ) : base(baseRepository) {
            _userRepository = userRepository;
            _incidentCodeMetadataRepo = incidentCodeMetadataRepo;
            _incidentApproverRepo = incidentApproverRepo;
        }

        public IEnumerable<IncidentCode> ExecuteProcess(GetAllIncidentCode filter)
        {
            var result = _repository.GetContextEntity().Include(ic => ic.IncidentApprovers).Include(ic => ic.IncidentCodeMetadata).ToList();

            return result;
        }

        public IncidentCode ExecuteProcess(CreateIncidentCode incidentCode)
        {
            var exist = _repository.GetById(incidentCode.Code);

            if (exist != null)
            {
                throw new BadHttpRequestException("El código de incidencia ya se encuentra registrado");
            }

            if (incidentCode.WithOperation && incidentCode.Metadata == null)
            {
                throw new BadHttpRequestException("La metadata es requerida");
            }

            if (incidentCode.RequiredApproval && (incidentCode.IncidentApprovers == null || !incidentCode.IncidentApprovers!.Any())) {
                throw new BadHttpRequestException("Se requiere al menos un usuario aprobador");
            }

            var newIncidentCode = new IncidentCode()
            {
                Code = incidentCode.Code,
                ExternalCode = incidentCode.ExternalCode,
                Label = incidentCode.Label,
                ApplyMode = incidentCode.ApplyMode,
                IsAdditional = incidentCode.IsAdditional,
                Notes = incidentCode.Notes,
                RequiredApproval = incidentCode.RequiredApproval,
                WithOperation = incidentCode.WithOperation,
            };

            if (incidentCode.WithOperation && incidentCode.Metadata is not null)
            {
                newIncidentCode.IncidentCodeMetadata = new IncidentCodeMetadata()
                {
                    Amount = incidentCode.Metadata.Amount ?? 0,
                    CustomValue = incidentCode.Metadata.Amount,
                    MathOperation = incidentCode.Metadata.MathOperation,
                    ColumnForOperation = incidentCode.Metadata.ColumnForOperation,
                };
            }

            if (incidentCode.RequiredApproval && incidentCode.IncidentApprovers != null)
            {
                var userForApproval = _userRepository.GetByFilter((u) => incidentCode.IncidentApprovers.Contains(u.Id.ToString())).ToList();
                newIncidentCode.IncidentApprovers = userForApproval.Select(user => new IncidentApprover {
                    UserId = user.Id,
                    IncidentCode = newIncidentCode.Code,
                    User = user,
                    ItemIncidentCode = newIncidentCode
                }).ToList();
            }

            var result = _repository.Create(newIncidentCode);
            _repository.Save();

            return result;
        }

        public IncidentCode ExecuteProcess(EditIncidentCode incidentCode)
        {
            var incident = _repository.GetById(incidentCode.Code);

            if (incident == null)
            {
                throw new BadHttpRequestException("El código de incidencia no se encuentra registrado");
            }

            if (incidentCode.WithOperation && incidentCode.Metadata == null)
            {
                throw new BadHttpRequestException("La metadata es requerida");
            }

            if (incidentCode.RequiredApproval && (incidentCode.IncidentApprovers == null || !incidentCode.IncidentApprovers!.Any()))
            {
                throw new BadHttpRequestException("Se requiere al menos un usuario aprobador");
            }

            incident.ExternalCode = incidentCode.ExternalCode;
            incident.Label = incidentCode.Label;
            incident.ApplyMode = incidentCode.ApplyMode;
            incident.IsAdditional = incidentCode.IsAdditional;
            incident.Notes = incidentCode.Notes;
            incident.RequiredApproval = incidentCode.RequiredApproval;
            incident.WithOperation = incidentCode.WithOperation;

            var incidentMetadata = _incidentCodeMetadataRepo.GetById(incident.MetadataId!);

            if (incidentCode.WithOperation && incidentCode.Metadata is not null)
            {
                if (incidentMetadata is not null)
                {
                    incidentMetadata.Amount = incidentCode.Metadata.Amount ?? 0;
                    incidentMetadata.CustomValue = incidentCode.Metadata.Amount;
                    incidentMetadata.MathOperation = incidentCode.Metadata.MathOperation;
                    incidentMetadata.ColumnForOperation = incidentCode.Metadata.ColumnForOperation;
                } else
                {
                    incidentMetadata = _incidentCodeMetadataRepo.Create(new IncidentCodeMetadata()
                    {
                        Amount = incidentCode.Metadata.Amount ?? 0,
                        CustomValue = incidentCode.Metadata.Amount,
                        MathOperation = incidentCode.Metadata.MathOperation,
                        ColumnForOperation = incidentCode.Metadata.ColumnForOperation,
                    });

                    incident.MetadataId = incidentMetadata.Id;
                }
            } else if (incidentMetadata is not null)
            {
                _incidentCodeMetadataRepo.Delete(incidentMetadata);
                incident.MetadataId = null;
            }

            var preIncidentApprovers = _incidentApproverRepo.GetByFilter(ia => ia.IncidentCode == incident.Code);

            foreach (var approver in preIncidentApprovers)
            {
                _incidentApproverRepo.Delete(approver);
            }
            var incidentApprovers = new List<IncidentApprover>();

            if (incidentCode.RequiredApproval && incidentCode.IncidentApprovers != null)
            {
                var userForApproval = _userRepository.GetByFilter((u) => incidentCode.IncidentApprovers.Contains(u.Id.ToString())).ToList();
                incidentApprovers = userForApproval.Select(user => new IncidentApprover
                {
                    UserId = user.Id,
                    IncidentCode = incident.Code,
                    User = user,
                    ItemIncidentCode = incident
                }).ToList();

                foreach (var approver in incidentApprovers)
                {
                    _incidentApproverRepo.Create(approver);
                }
            }

            if (incidentMetadata is not null)
            {
                _incidentCodeMetadataRepo.Save();
            }

            if ((preIncidentApprovers is not null && preIncidentApprovers.Any()) || incidentApprovers.Any())
            {
                _incidentApproverRepo.Save();
            }

            _repository.Update(incident);
            _repository.Save();

            incident.IncidentApprovers = incidentApprovers;

            if (incident.MetadataId is not null)
            {
                incident.IncidentCodeMetadata = incidentMetadata;
            }

            return incident;
        }
    }
}
