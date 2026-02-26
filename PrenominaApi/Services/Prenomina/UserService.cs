using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PrenominaApi.Helper;
using PrenominaApi.Models;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Repositories;
using PrenominaApi.Repositories.Prenomina;

namespace PrenominaApi.Services.Prenomina
{
    public class UserService : ServicePrenomina<User>
    {
        private readonly IPasswordHasher<HasPassword> _passwordHasher;
        private readonly IBaseRepositoryPrenomina<UserCompany> _userCompanyRepository;
        private readonly IBaseRepositoryPrenomina<UserDepartment> _userDepartmentRespository;
        private readonly IBaseRepositoryPrenomina<UserSupervisor> _userSupervisorRepository;
        private readonly IBaseRepositoryPrenomina<Role> _roleRepository;
        private readonly IBaseRepository<Models.Company> _companyRepository;
        private readonly IBaseRepository<Center> _centerRepository;
        private readonly IBaseRepository<Supervisor> _supervisorRespository;
        private readonly IConfiguration _configuration;
        private readonly GlobalPropertyService _globalPropertyService;
        private readonly ICacheService _cacheService;

        public UserService(
            IBaseRepositoryPrenomina<User> baseRepository,
            IBaseRepositoryPrenomina<UserCompany> userCompanyRepository,
            IBaseRepositoryPrenomina<UserDepartment> userDepartmentRespository,
            IBaseRepositoryPrenomina<UserSupervisor> userSupervisorRepository,
            IBaseRepositoryPrenomina<Role> roleRepository,
            IBaseRepository<Models.Company> companyRepository,
            IBaseRepository<Center> centerRepository,
            IBaseRepository<Supervisor> supervisorRespository,
            IPasswordHasher<HasPassword> passwordHasher,
            IConfiguration configuration,
            GlobalPropertyService globalPropertyService,
            ICacheService cacheService
        ) : base(baseRepository)
        {
            _userCompanyRepository = userCompanyRepository;
            _userSupervisorRepository = userSupervisorRepository;
            _userDepartmentRespository = userDepartmentRespository;
            _roleRepository = roleRepository;
            _companyRepository = companyRepository;
            _centerRepository = centerRepository;
            _supervisorRespository = supervisorRespository;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
            _globalPropertyService = globalPropertyService;
            _cacheService = cacheService;
        }

        public IEnumerable<User> ExecuteProcess(GetAllUser getAllUser)
        {
            // Optimizado: proyección selectiva en lugar de múltiples Includes
            var result = _repository.GetContextEntity()
                .AsNoTracking()
                .Include(u => u.Role)
                .Include(u => u.Companies)
                    .ThenInclude(c => c.UserDepartments)
                .Include(u => u.Companies)
                    .ThenInclude(c => c.UserSupervisors)
                .AsSplitQuery() // Dividir en múltiples queries para evitar explosión cartesiana
                .ToList();

            return result;
        }

        public User ExecuteProcess(CreateUser user)
        {
            var typeSystem = _globalPropertyService.TypeTenant;
            string hasPassword = _passwordHasher.HashPassword(user, user.Password);

            // Obtener rol con caché
            var rol = _cacheService.GetOrCreate(
                $"role_{user.RoleId}",
                () => _roleRepository.GetById(user.RoleId),
                TimeSpan.FromMinutes(30)
            );

            if (rol == null)
            {
                throw new BadHttpRequestException("El rol no existe");
            }

            if (rol.Code != RoleCode.Sudo && (user.Companies == null || !user.Companies!.Any()))
            {
                throw new BadHttpRequestException("El campo empresa es requerido");
            }

            // Verificar email existente con query optimizada
            var emailExists = _repository.GetContextEntity()
                .AsNoTracking()
                .Any(item => item.Email.ToLower() == user.Email.ToLower());

            if (emailExists)
            {
                throw new BadHttpRequestException("El correo electrónico ya se encuentra registrado");
            }

            var created = _repository.Create(new User()
            {
                Name = user.Name,
                Email = user.Email,
                Password = hasPassword,
                RoleId = user.RoleId,
            });

            if (user.Companies != null && user.Companies.Any())
            {
                foreach (var company in user.Companies)
                {
                    var companyCreated = _userCompanyRepository.Create(new UserCompany()
                    {
                        CompanyId = company.Id,
                        UserId = created.Id,
                    });

                    foreach (var tenantId in company.TenantIds)
                    {
                        if (typeSystem == TypeTenant.Supervisor)
                        {
                            _userSupervisorRepository.Create(new UserSupervisor()
                            {
                                SupervisorId = tenantId,
                                UserCompanyId = companyCreated.Id,
                            });
                        }
                        else
                        {
                            _userDepartmentRespository.Create(new UserDepartment()
                            {
                                DepartmentCode = tenantId.ToString(),
                                UserCompanyId = companyCreated.Id,
                            });
                        }
                    }
                }
            }

            _repository.Save();

            return created;
        }

        public User ExecuteProcess(EditUser editUser)
        {
            var findUser = _repository.GetById(Guid.Parse(editUser.UserId!));
            var typeSystem = _globalPropertyService.TypeTenant;

            if (findUser is null)
            {
                throw new BadHttpRequestException("El usuario no existe", 404);
            }

            if (findUser.Email == SysConfig.UserDefault)
            {
                throw new BadHttpRequestException("El usuario no puede ser modificado");
            }

            var rol = _roleRepository.GetById(editUser.RoleId);

            if (rol == null)
            {
                throw new BadHttpRequestException("El rol no existe");
            }

            if (rol.Code != RoleCode.Sudo && (editUser.Companies == null || !editUser.Companies!.Any()))
            {
                throw new BadHttpRequestException("El campo empresa es requerido");
            }

            // Verificar email con query optimizada
            var emailExists = _repository.GetContextEntity()
                .AsNoTracking()
                .Any(item => item.Email.ToLower() == editUser.Email.ToLower() && item.Id != findUser.Id);

            if (emailExists)
            {
                throw new BadHttpRequestException("El correo electrónico ya se encuentra registrado");
            }

            findUser.Name = editUser.Name;
            findUser.Email = editUser.Email;
            findUser.RoleId = editUser.RoleId;

            if (!string.IsNullOrEmpty(editUser.Password))
            {
                string hasPassword = _passwordHasher.HashPassword(editUser, editUser.Password);
                findUser.Password = hasPassword;
            }

            var updated = _repository.Update(findUser);

            // Optimizado: obtener IDs de UserCompany primero
            var userCompanyIds = _userCompanyRepository.GetContextEntity()
                .AsNoTracking()
                .Where(uc => uc.UserId == findUser.Id)
                .Select(uc => uc.Id)
                .ToList();

            // Eliminar en batch
            _userSupervisorRepository.GetContextEntity()
                .Where(us => userCompanyIds.Contains(us.UserCompanyId))
                .ExecuteDelete();

            _userDepartmentRespository.GetContextEntity()
                .Where(ud => userCompanyIds.Contains(ud.UserCompanyId))
                .ExecuteDelete();

            _userCompanyRepository.GetContextEntity()
                .Where(uc => uc.UserId == findUser.Id)
                .ExecuteDelete();

            if (editUser.Companies != null && editUser.Companies.Any())
            {
                foreach (var company in editUser.Companies)
                {
                    var companyCreated = _userCompanyRepository.Create(new UserCompany()
                    {
                        CompanyId = company.Id,
                        UserId = updated.Id,
                    });

                    foreach (var tenantId in company.TenantIds)
                    {
                        if (typeSystem == TypeTenant.Supervisor)
                        {
                            _userSupervisorRepository.Create(new UserSupervisor()
                            {
                                SupervisorId = tenantId,
                                UserCompanyId = companyCreated.Id,
                            });
                        }
                        else
                        {
                            _userDepartmentRespository.Create(new UserDepartment()
                            {
                                DepartmentCode = tenantId.ToString(),
                                UserCompanyId = companyCreated.Id,
                            });
                        }
                    }
                }
            }

            _repository.Save();

            return updated;
        }

        public ResultLogin ExecuteProcess(AuthLogin authLogin)
        {
            // Optimizado: proyección selectiva
            var findUser = _repository.GetContextEntity()
                .AsNoTracking()
                .Include(u => u.Role)
                .Where(user => user.Email.Trim().ToLower() == authLogin.Email.Trim().ToLower())
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.Password,
                    u.Name,
                    RoleCode = u.Role!.Code,
                    u.RoleId
                })
                .FirstOrDefault();

            var typeSystem = _globalPropertyService.TypeTenant;

            if (findUser != null)
            {
                var userHasPassword = new HasPassword() { Password = authLogin.Password };
                var validPassword = _passwordHasher.VerifyHashedPassword(userHasPassword, findUser.Password, authLogin.Password);

                if (validPassword == PasswordVerificationResult.Success)
                {
                    // Reconstruir objeto User mínimo para JWT
                    var userForToken = new User
                    {
                        Id = findUser.Id,
                        Email = findUser.Email,
                        Name = findUser.Name,
                        Password = findUser.Password,
                        RoleId = findUser.RoleId,
                    };

                    var token = JwtSecurityToken.CreateJwt(
                        userForToken,
                        _configuration.GetValue<string>("Jwt:Key") ?? "",
                        _configuration.GetValue<string>("Jwt:Issuer") ?? "",
                        _configuration.GetValue<int>("Jwt:Duration")
                    );

                    return new ResultLogin()
                    {
                        Token = token,
                        Username = findUser.Email,
                        UserDetails = ExecuteProcess<string, UserDetails>(findUser.Id.ToString()),
                        TypeTenant = typeSystem,
                        Year = _globalPropertyService.YearOfOperation
                    };
                }
            }

            throw new UnauthorizedAccessException("El correo o contraseña es incorrecto");
        }

        public UserDetails ExecuteProcess(string userId)
        {
            var result = new UserDetails()
            {
                Companies = new List<Company>(),
                Centers = Enumerable.Empty<Center>(),
                Supervisors = Enumerable.Empty<Supervisor>()
            };

            var findUser = _repository.GetById(Guid.Parse(userId));

            if (findUser == null)
            {
                throw new BadHttpRequestException("El usuario no fue encontrado");
            }

            // Obtener rol con secciones
            var rol = _roleRepository.GetContextEntity()
                .AsNoTracking()
                .Include(r => r.Sections)
                .Where(r => r.Id == findUser.RoleId)
                .FirstOrDefault();

            if (rol == null)
            {
                throw new BadHttpRequestException("Rol no encontrado");
            }

            rol.Users = new List<User>();

            if (rol.Code == RoleCode.Sudo)
            {
                // Para superusuario, obtener todo con caché
                var companies = _cacheService.GetOrCreate(
                    CacheKeys.Companies,
                    () => _companyRepository.GetAll().ToList(),
                    TimeSpan.FromMinutes(30)
                );

                // Obtener centros que tienen keys asociados
                var allCenters = _centerRepository.GetContextEntity()
                    .AsNoTracking()
                    .Include(c => c.Keys)
                    .Where(c => c.Keys != null && c.Keys.Any())
                    .Select(c => new Center
                    {
                        Id = c.Id,
                        Company = c.Company,
                        DepartmentName = c.DepartmentName
                    })
                    .ToList();

                var allSupervisors = _supervisorRespository.GetAll().ToList();

                result.Companies = companies;
                result.Centers = allCenters;
                result.Supervisors = allSupervisors;
            }
            else
            {
                // Para usuarios normales, obtener solo sus empresas asignadas
                var userCompanies = _userCompanyRepository.GetContextEntity()
                    .AsNoTracking()
                    .Include(uc => uc.UserDepartments)
                    .Include(uc => uc.UserSupervisors)
                    .Where(uc => uc.UserId == findUser.Id)
                    .ToList();

                var companyIds = userCompanies.Select(uc => uc.CompanyId).ToHashSet();
                var companies = _companyRepository.GetAll()
                    .Where(c => companyIds.Contains((int)c.Id))
                    .ToList();

                var allDepartmentCodes = userCompanies
                    .SelectMany(uc => uc.UserDepartments ?? Enumerable.Empty<UserDepartment>())
                    .Select(ud => ud.DepartmentCode.Trim())
                    .ToHashSet();

                var allSupervisorIds = userCompanies
                    .SelectMany(uc => uc.UserSupervisors ?? Enumerable.Empty<UserSupervisor>())
                    .Select(us => (decimal)us.SupervisorId)
                    .ToHashSet();

                // Obtener centros y supervisores en queries separadas
                var centers = _centerRepository.GetContextEntity()
                    .AsNoTracking()
                    .Where(c => allDepartmentCodes.Contains(c.Id.Trim()) && companyIds.Contains((int)c.Company))
                    .ToList();

                var supervisors = _supervisorRespository.GetContextEntity()
                    .AsNoTracking()
                    .Where(s => allSupervisorIds.Contains(s.Id) && companyIds.Contains((int)s.Company))
                    .ToList();

                result.Companies = companies;
                result.Centers = centers;
                result.Supervisors = supervisors;
            }

            result.role = rol;

            return result;
        }

        public Models.Dto.Output.InitCreateUser ExecuteProcess(Models.Dto.Input.InitCreateUser initCreate)
        {
            // Usar caché para datos estáticos
            var companies = _cacheService.GetOrCreate(
                CacheKeys.Companies,
                () => _companyRepository.GetAll().ToList(),
                TimeSpan.FromMinutes(30)
            );

            var centers = _centerRepository.GetContextEntity()
                .AsNoTracking()
                .Where(item => item.Keys != null && item.Keys.Any() &&
                               !string.IsNullOrEmpty(item.Id) &&
                               !string.IsNullOrWhiteSpace(item.Id))
                .ToList();

            var supervisors = _supervisorRespository.GetAll();

            var roles = _cacheService.GetOrCreate(
                CacheKeys.Roles,
                () => _roleRepository.GetAll().ToList(),
                TimeSpan.FromMinutes(60)
            );

            return new Models.Dto.Output.InitCreateUser()
            {
                Companies = companies,
                Centers = centers,
                Supervisors = supervisors,
                roles = roles
            };
        }

        public IEnumerable<User> ExecuteProcess(GetUserByPermissionSection getUserByPermissionSection)
        {
            // Optimizado: obtener roles con secciones de permisos
            var validRoleIds = _roleRepository.GetContextEntity()
                .AsNoTracking()
                .Include(r => r.Sections)
                .Where(r => r.Code == RoleCode.Sudo ||
                            (r.Sections != null &&
                             r.Sections.Any(s => s.SectionsCode == SectionCode.PendingsAttendanceIncident)))
                .Select(r => r.Id)
                .ToHashSet();

            var users = _repository.GetContextEntity()
                .AsNoTracking()
                .Where(user => validRoleIds.Contains(user.RoleId))
                .ToList();

            return users;
        }

        public IEnumerable<string> ExecuteProcess(GetTenantsUserByCompany getSectionsUserByCompany)
        {
            var findUser = _repository.GetById(Guid.Parse(getSectionsUserByCompany.UserId));

            if (findUser == null)
            {
                throw new BadHttpRequestException("Usuario no encontrado");
            }

            var rol = _roleRepository.GetById(findUser.RoleId);

            if (rol == null)
            {
                throw new BadHttpRequestException("Rol no encontrado");
            }

            if (rol.Code == RoleCode.Sudo)
            {
                // Superusuario: devolver todos los tenants
                if (_globalPropertyService.TypeTenant == TypeTenant.Department)
                {
                    return _centerRepository.GetContextEntity()
                        .AsNoTracking()
                        .Include(c => c.Keys)
                        .Where(c => c.Keys != null && c.Keys.Any())
                        .Select(d => d.Id.Trim())
                        .ToList();
                }

                return _supervisorRespository.GetAll()
                    .Select(s => s.Id.ToString())
                    .ToList();
            }

            // Usuario normal: verificar acceso a empresa
            var findUserCompany = _userCompanyRepository.GetContextEntity()
                .AsNoTracking()
                .Where(uc => uc.UserId == Guid.Parse(getSectionsUserByCompany.UserId) &&
                             uc.CompanyId == getSectionsUserByCompany.CompanyId)
                .FirstOrDefault();

            if (findUserCompany == null)
            {
                throw new BadHttpRequestException("El usuario no tiene acceso a esta empresa");
            }

            if (_globalPropertyService.TypeTenant == TypeTenant.Department)
            {
                return _userDepartmentRespository.GetContextEntity()
                    .AsNoTracking()
                    .Where(d => d.UserCompanyId == findUserCompany.Id)
                    .Select(d => d.DepartmentCode.Trim())
                    .ToList();
            }

            return _userSupervisorRepository.GetContextEntity()
                .AsNoTracking()
                .Where(s => s.UserCompanyId == findUserCompany.Id)
                .Select(s => s.SupervisorId.ToString())
                .ToList();
        }

        public bool ExecuteProcess(DeleteUser deleteUser)
        {
            var findUser = _repository.GetById(Guid.Parse(deleteUser.UserId));

            if (findUser is null)
            {
                throw new BadHttpRequestException("El usuario no fue encontrado");
            }

            if (findUser.Email == SysConfig.UserDefault)
            {
                throw new BadHttpRequestException("El usuario no puede ser eliminado");
            }

            _repository.Delete(findUser);
            _repository.Save();

            // Invalidar caché relacionado
            _cacheService.RemoveByPrefix("user_");

            return true;
        }
    }
}
