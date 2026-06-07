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
        public readonly IBaseRepositoryPrenomina<IncidentCodeAllowedRoles> _incidentCodeAllowedRolesRepo;
        public readonly IBaseRepositoryPrenomina<IncidentApprovalStep> _approvalStepRepo;
        private readonly ICacheService _cacheService;

        public IncidentCodeService(
            IBaseRepositoryPrenomina<IncidentCode> baseRepository,
            IBaseRepositoryPrenomina<User> userRepository,
            IBaseRepositoryPrenomina<IncidentCodeMetadata> incidentCodeMetadataRepo,
            IBaseRepositoryPrenomina<IncidentApprover> incidentApproverRepo,
            IBaseRepositoryPrenomina<IncidentCodeAllowedRoles> incidentCodeAllowedRolesRepo,
            IBaseRepositoryPrenomina<IncidentApprovalStep> approvalStepRepo,
            ICacheService cacheService
        ) : base(baseRepository) {
            _userRepository = userRepository;
            _incidentCodeMetadataRepo = incidentCodeMetadataRepo;
            _incidentApproverRepo = incidentApproverRepo;
            _incidentCodeAllowedRolesRepo = incidentCodeAllowedRolesRepo;
            _approvalStepRepo = approvalStepRepo;
            _cacheService = cacheService;
        }

        public IEnumerable<IncidentCode> ExecuteProcess(GetAllIncidentCode filter)
        {
            var result = _repository.GetContextEntity().Include(ic => ic.IncidentApprovers).Include(ic => ic.IncidentCodeAllowedRoles).Include(ic => ic.IncidentCodeMetadata).ToList();

            // Adjuntar la cadena de firmas (plantilla global por código) para edición.
            var codes = result.Select(r => r.Code).ToList();
            var stepsByCode = _approvalStepRepo.GetByFilter(s => codes.Contains(s.IncidentCode))
                .ToList()
                .GroupBy(s => s.IncidentCode)
                .ToDictionary(g => g.Key, g => g.OrderBy(s => s.StepOrder).ToList());

            foreach (var code in result)
            {
                code.ApprovalSteps = stepsByCode.TryGetValue(code.Code, out var steps)
                    ? steps
                    : new List<IncidentApprovalStep>();
            }

            return result;
        }

        // Reemplaza la cadena de firmas de un código por la enviada (orden según la lista).
        private void ReplaceApprovalSteps(string incidentCode, List<ApprovalStepInput>? steps)
        {
            var existing = _approvalStepRepo.GetByFilter(s => s.IncidentCode == incidentCode).ToList();
            foreach (var step in existing)
            {
                _approvalStepRepo.Delete(step);
            }

            if (steps != null)
            {
                var order = 1;
                foreach (var step in steps.OrderBy(s => s.StepOrder))
                {
                    _approvalStepRepo.Create(new IncidentApprovalStep
                    {
                        IncidentCode = incidentCode,
                        StepOrder = order++,
                        RoleId = step.RoleId,
                        Scope = step.Scope,
                        Mode = step.Mode,
                        IsOptional = step.IsOptional,
                    });
                }
            }

            _approvalStepRepo.Save();
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
                RestrictedWithRoles = incidentCode.RestrictedWithRoles,
                AvailableForTimeOff = incidentCode.AvailableForTimeOff
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

            if (incidentCode.RestrictedWithRoles && incidentCode.AllowedRoles != null)
            {
                newIncidentCode.IncidentCodeAllowedRoles = incidentCode.AllowedRoles.Select(roleId => new IncidentCodeAllowedRoles
                {
                    RoleId = Guid.Parse(roleId),
                    IncidentCode = newIncidentCode.Code,
                    ItemIncidentCode = newIncidentCode
                }).ToList();
            }

            var result = _repository.Create(newIncidentCode);
            _repository.Save();

            ReplaceApprovalSteps(newIncidentCode.Code, incidentCode.ApprovalSteps);

            _cacheService.Remove(CacheKeys.IncidentCodes);

            return result;
        }

        public IncidentCode ExecuteProcess(EditIncidentCode incidentCode)
        {
            using var transaction = _repository.GetDbContext().Database.BeginTransaction();

            try 
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

                if (incidentCode.RestrictedWithRoles && (incidentCode.AllowedRoles == null || !incidentCode.AllowedRoles!.Any()))
                {
                    throw new BadHttpRequestException("Se requiere al menos un rol permitido");
                }

                incident.ExternalCode = incidentCode.ExternalCode;
                incident.Label = incidentCode.Label;
                incident.ApplyMode = incidentCode.ApplyMode;
                incident.IsAdditional = incidentCode.IsAdditional;
                incident.Notes = incidentCode.Notes;
                incident.RequiredApproval = incidentCode.RequiredApproval;
                incident.WithOperation = incidentCode.WithOperation;
                incident.RestrictedWithRoles = incidentCode.RestrictedWithRoles;
                incident.AvailableForTimeOff = incidentCode.AvailableForTimeOff;

                var incidentMetadata = _incidentCodeMetadataRepo.GetById(incident.MetadataId!);

                if (incidentCode.WithOperation && incidentCode.Metadata is not null)
                {
                    if (incidentMetadata is not null)
                    {
                        incidentMetadata.Amount = incidentCode.Metadata.Amount ?? 0;
                        incidentMetadata.CustomValue = incidentCode.Metadata.Amount;
                        incidentMetadata.MathOperation = incidentCode.Metadata.MathOperation;
                        incidentMetadata.ColumnForOperation = incidentCode.Metadata.ColumnForOperation;
                    }
                    else
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
                }
                else if (incidentMetadata is not null)
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

                var allowedRolesPre = _incidentCodeAllowedRolesRepo.GetByFilter(ar => ar.IncidentCode == incident.Code);

                foreach (var role in allowedRolesPre)
                {
                    _incidentCodeAllowedRolesRepo.Delete(role);
                }

                if (incidentCode.RestrictedWithRoles && incidentCode.AllowedRoles != null)
                {
                    foreach (var roleId in incidentCode.AllowedRoles)
                    {
                        var allowedRole = new IncidentCodeAllowedRoles
                        {
                            RoleId = Guid.Parse(roleId),
                            IncidentCode = incident.Code,
                            ItemIncidentCode = incident
                        };
                        _incidentCodeAllowedRolesRepo.Create(allowedRole);
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

                if ((allowedRolesPre is not null && allowedRolesPre.Any()) || (incidentCode.RestrictedWithRoles && incidentCode.AllowedRoles != null && incidentCode.AllowedRoles.Any()))
                {
                    _incidentCodeAllowedRolesRepo.Save();
                }

                _repository.Update(incident);
                _repository.Save();

                ReplaceApprovalSteps(incident.Code, incidentCode.ApprovalSteps);

                incident.IncidentApprovers = incidentApprovers;

                if (incident.MetadataId is not null)
                {
                    incident.IncidentCodeMetadata = incidentMetadata;
                }

                transaction.Commit();
                _cacheService.Remove(CacheKeys.IncidentCodes);

                return incident;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
