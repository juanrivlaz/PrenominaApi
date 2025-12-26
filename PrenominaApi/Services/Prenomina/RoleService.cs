using Microsoft.EntityFrameworkCore;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Repositories.Prenomina;

namespace PrenominaApi.Services.Prenomina
{
    public class RoleService : ServicePrenomina<Role>
    {
        private readonly IBaseRepositoryPrenomina<Section> _sectionRepository;
        public RoleService(
            IBaseRepositoryPrenomina<Role> baseRepository,
            IBaseRepositoryPrenomina<Section> sectionRepository
        ) : base(baseRepository)
        {
            _sectionRepository = sectionRepository;
        }
        
        public override IEnumerable<Role> GetAll()
        {
            var result = _repository.GetContextEntity().Include(r => r.Sections).ToList();

            return result;
        }

        public Role ExecuteProcess(CreateRole role)
        {
            string codeRole = string.Concat(role.Label.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(word => word.Substring(0, Math.Min(5, word.Length))).Select(fragment => fragment.ToLower()));

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
                }

                return new SectionRol()
                {
                    SectionsCode = s.Code,
                    RolesId = result.Id,
                    Permissions = permissions
                };
            }).ToList();

            _repository.Save();

            return result;
        }
    }
}
