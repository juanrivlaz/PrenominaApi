using Microsoft.EntityFrameworkCore;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Input.Role;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Repositories.Prenomina;

namespace PrenominaApi.Services.Prenomina
{
    public class RoleService : ServicePrenomina<Role>
    {
        private readonly IBaseRepositoryPrenomina<Section> _sectionRepository;
        private readonly IBaseRepositoryPrenomina<SectionRol> _sectionRoleRepository;
        private readonly ICacheService _cacheService;

        public RoleService(
            IBaseRepositoryPrenomina<Role> baseRepository,
            IBaseRepositoryPrenomina<Section> sectionRepository,
            IBaseRepositoryPrenomina<SectionRol> sectionRoleRepository,
            ICacheService cacheService
        ) : base(baseRepository)
        {
            _sectionRepository = sectionRepository;
            _sectionRoleRepository = sectionRoleRepository;
            _cacheService = cacheService;
        }
        
        public override IEnumerable<Role> GetAll()
        {
            var result = _repository.GetContextEntity().Include(r => r.Sections).ToList();

            return result;
        }

        public Role ExecuteProcess(CreateRole role)
        {
            string codeRole = string.Concat(role.Label.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(word => word.Substring(0, Math.Min(8, word.Length))).Select(fragment => fragment.ToLower()));

            var existCode = _repository.GetByFilter(item => item.Code == codeRole).FirstOrDefault();

            if (existCode != null)
            {
                throw new BadHttpRequestException("El nombre del rol ya se encuentra registrado");
            }

            foreach (var item in role.Sections)
            {
                var existSection = _sectionRepository.GetById(item.Code);

                if (existSection == null)
                {
                    _sectionRepository.Create(new Section() {
                        Code = item.Code,
                        Name = item.Label,
                        Description = item.Label
                    });
                }
            }

            _sectionRepository.Save();
            var sectionCode = role.Sections.Select(item => item.Code).ToList();
            var sections = _sectionRepository.GetByFilter(item => sectionCode.Contains(item.Code)).ToList();

            Role result = _repository.Create(new Role()
            {
                Code = codeRole,
                Label = role.Label,
            });

            result.Sections = sections.Select(s =>
            {
                var permissions = new Dictionary<string, bool>
                    {
                        { "Read", true },
                        { "Write", true },
                        { "Delete", true }
                    };

                if (s.Code == SectionCode.Attendance.ToLower())
                {
                    permissions.Add("CanClosePayrollPeriod", role.CanClosePayrollPeriod);
                    permissions.Add("CanModifyCheckins", role.CanModifyCheckins);
                }

                if (s.Code == SectionCode.Periods.ToLower())
                {
                    permissions.Add("CanManagePeriods", role.CanManagePeriods);
                }

                return new SectionRol()
                {
                    SectionsCode = s.Code,
                    RolesId = result.Id,
                    Permissions = permissions
                };
            }).ToList();

            _repository.Save();
            _cacheService.Remove(CacheKeys.Roles);

            return result;
        }

        public Role ExecuteProcess(EditRole role)
        {

            using var transaction = _repository.GetDbContext().Database.BeginTransaction();

            try
            {
                string codeRole = string.Concat(role.Label.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(word => word.Substring(0, Math.Min(8, word.Length))).Select(fragment => fragment.ToLower()));

                var existRole = _repository.GetById(Guid.Parse(role.RoleId));

                if (existRole == null)
                {
                    throw new BadHttpRequestException("El rol no se encuentra registrado");
                }

                var existCode = _repository.GetByFilter(item => item.Code == codeRole && item.Id != existRole.Id).Any();

                if (existCode)
                {
                    throw new BadHttpRequestException("El nombre del rol ya se encuentra registrado");
                }

                existRole.Code = codeRole;
                existRole.Label = role.Label;
                existRole.UpdatedAt = DateTime.UtcNow;

                var requestedSectionCodes = role.Sections.Select(s => s.Code).ToList();
                var requestedCodeSet = requestedSectionCodes.ToHashSet();

                var existingSections = _sectionRepository.GetByFilter(s => requestedSectionCodes.Contains(s.Code)).ToList();
                var existingSectionCodes = existingSections.Select(s => s.Code).ToHashSet();

                var newSections = role.Sections
                    .Where(s => !existingSectionCodes.Contains(s.Code))
                    .Select(s => new Section
                    {
                        Code = s.Code,
                        Name = s.Label,
                        Description = s.Label
                    }).ToList();

                foreach (var newSection in newSections)
                {
                    _sectionRepository.Create(newSection);
                }

                Dictionary<string, bool> BuildPermissions(string sectionCode)
                {
                    var permissions = new Dictionary<string, bool>
                    {
                        { "Read", true },
                        { "Write", true },
                        { "Delete", true }
                    };

                    if (sectionCode == SectionCode.Attendance.ToLower())
                    {
                        permissions.Add("CanClosePayrollPeriod", role.CanClosePayrollPeriod);
                        permissions.Add("CanModifyCheckins", role.CanModifyCheckins);
                    }

                    if (sectionCode == SectionCode.Periods.ToLower())
                    {
                        permissions.Add("CanManagePeriods", role.CanManagePeriods);
                    }

                    return permissions;
                }

                var prevSectionRoles = _sectionRoleRepository.GetByFilter(sr => sr.RolesId == existRole.Id).ToList();
                var prevByCode = prevSectionRoles.ToDictionary(sr => sr.SectionsCode);

                foreach (var prev in prevSectionRoles.Where(p => !requestedCodeSet.Contains(p.SectionsCode)))
                {
                    _sectionRoleRepository.Delete(prev);
                }

                foreach (var code in requestedSectionCodes)
                {
                    if (prevByCode.TryGetValue(code, out var existingLink))
                    {
                        existingLink.Permissions = BuildPermissions(code);
                        existingLink.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        _sectionRoleRepository.Create(new SectionRol
                        {
                            SectionsCode = code,
                            RolesId = existRole.Id,
                            Permissions = BuildPermissions(code)
                        });
                    }
                }

                _repository.GetDbContext().SaveChanges();

                transaction.Commit();
                _cacheService.Remove(CacheKeys.Roles);
                _cacheService.RemoveByPrefix("role_");

                return existRole;

            } catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
