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
            GlobalPropertyService globalPropertyService
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
        }

        public IEnumerable<User> ExecuteProcess(GetAllUser getAllUser)
        {
            var result = _repository.GetContextEntity().Include(u => u.Role).Include(u => u.Companies).ThenInclude(c => c.UserDepartments).Include(u => u.Companies).ThenInclude(c => c.UserSupervisors).ToList();

            return result;
        }

        public User ExecuteProcess(CreateUser user)
        {
            var typeSystem = _globalPropertyService.TypeTenant;
            string hasPassword = _passwordHasher.HashPassword(user, user.Password);
            var rol = _roleRepository.GetById(user.RoleId);

            if (rol == null)
            {
                throw new BadHttpRequestException("El rol no existe");
            }

            if (rol.Code != RoleCode.Sudo && (user.Companies == null || !user.Companies!.Any()))
            {
                throw new BadHttpRequestException("El campo empresa es requerido");
            }
            var existEmail = _repository.GetByFilter((item) => item.Email.ToString() == user.Email.ToString()).FirstOrDefault();

            if (existEmail != null)
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
            var rol = _roleRepository.GetById(editUser.RoleId);

            if (findUser is null)
            {
                throw new BadHttpRequestException("El usuario no existe", 404);
            }

            if (findUser.Email == SysConfig.UserDefault)
            {
                throw new BadHttpRequestException("El usuario no puede ser modificado");
            }

            if (rol == null)
            {
                throw new BadHttpRequestException("El rol no existe");
            }

            if (rol.Code != RoleCode.Sudo && (editUser.Companies == null || !editUser .Companies!.Any()))
            {
                throw new BadHttpRequestException("El campo empresa es requerido");
            }

            var existEmail = _repository.GetByFilter((item) => item.Email.ToString() == editUser.Email.ToString() && item.Id != findUser.Id).FirstOrDefault();

            if (existEmail != null)
            {
                throw new BadHttpRequestException("El correo electrónico ya se encuentra registrado");
            }

            findUser.Name = editUser.Name;
            findUser.Email = editUser.Email;
            findUser.RoleId = editUser.RoleId;

            if (editUser.Password != String.Empty)
            {
                string hasPassword = _passwordHasher.HashPassword(editUser, editUser.Password);
                findUser.Password = hasPassword;
            }

            var updated = _repository.Update(findUser);

            _userSupervisorRepository.GetContextEntity().Where(us => _userCompanyRepository.GetContextEntity().Any(uc => uc.Id == us.UserCompanyId && uc.UserId == findUser.Id)).ExecuteDelete();
            _userDepartmentRespository.GetContextEntity().Where(ud => _userCompanyRepository.GetContextEntity().Any(uc => uc.Id == ud.UserCompanyId && uc.UserId == findUser.Id)).ExecuteDelete();
            _userCompanyRepository.GetContextEntity().Where(uc => uc.UserId == findUser.Id).ExecuteDelete();

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

            _userSupervisorRepository.Save();
            _userDepartmentRespository.Save();
            _userCompanyRepository.Save();
            _repository.Save();

            return updated;
        }

        public ResultLogin ExecuteProcess(AuthLogin authLogin)
        {
            var findUser = _repository.GetContextEntity().Include(u => u.Role).Where(user => user.Email.Trim().ToLower() == authLogin.Email.Trim().ToLower()).FirstOrDefault();
            var typeSystem = _globalPropertyService.TypeTenant;

            if (findUser != null)
            {
                var userHasPasswors = new HasPassword() { Password = authLogin.Password };
                var validPassword = _passwordHasher.VerifyHashedPassword(userHasPasswors, findUser.Password, authLogin.Password);

                if (validPassword == PasswordVerificationResult.Success)
                {
                    var token = JwtSecurityToken.CreateJwt(
                        findUser,
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
                Companies = [],
                Centers = [],
                Supervisors = []
            };

            var findUser = _repository.GetById(Guid.Parse(userId));

            if (findUser != null)
            {
                Role rol = _roleRepository.GetContextEntity().Include(r => r.Sections).Where(r => r.Id == findUser.RoleId).First();
                rol.Users = [];

                if (rol.Code == RoleCode.Sudo)
                {
                    var companies = _companyRepository.GetAll();
                    var allCenters = _centerRepository.GetContextEntity().Include(c => c.Keys).Where(c => c.Keys != null && c.Keys.Any());
                    var allSupervisor = _supervisorRespository.GetAll();

                    foreach (var company in companies)
                    {
                        result.Companies.Add(company);
                        var centers = allCenters.Where(c => c.Company == company.Id).Select((c) => new Center { Id = c.Id, Company = c.Company, DepartmentName = c.DepartmentName });
                        var supervisorsFind = allSupervisor.Where(s => s.Company == company.Id);
                        result.Supervisors = result.Supervisors.Concat(supervisorsFind);
                        result.Centers = result.Centers.Concat(centers);
                    }
                } else
                {
                    List<UserCompany> findCompanies = _userCompanyRepository.GetContextEntity().Include(uc => uc.UserDepartments).Include(uc => uc.UserSupervisors).Where(uc => uc.UserId == findUser.Id).ToList();
                    foreach (UserCompany userCompany in findCompanies)
                    {
                        var departmentsCodes = (userCompany.UserDepartments?.ToList() ?? []).Select(ud => ud.DepartmentCode).ToList();
                        var supervisorsIds = (userCompany.UserSupervisors?.ToList() ?? []).Select(us => Convert.ToDecimal(us.SupervisorId)).ToList();
                        var company = _companyRepository.GetById(Convert.ToDecimal(userCompany.CompanyId));
                        if (company != null)
                        {
                            result.Companies.Add(company);
                            var centers = _centerRepository.GetByFilter(c => departmentsCodes.Contains(c.Id.Trim()) && c.Company == company.Id);
                            var supervisorsFind = _supervisorRespository.GetByFilter(s => supervisorsIds.Contains(s.Id) && s.Company == company.Id);
                            result.Supervisors = result.Supervisors.Concat(supervisorsFind);
                            result.Centers = result.Centers.Concat(centers);
                        }
                    }
                }

                result.role = rol;

                return result;
            }

            throw new BadHttpRequestException("El usuario no fue encontrado");
        }

        public Models.Dto.Output.InitCreateUser ExecuteProcess(Models.Dto.Input.InitCreateUser initCreate)
        {
            var companies = _companyRepository.GetAll();
            var centers = _centerRepository.GetContextEntity().Where((item) => item.Keys != null && item.Keys.Any() && !string.IsNullOrEmpty(item.Id) && !string.IsNullOrWhiteSpace(item.Id)).AsNoTracking().ToList();
            var supervisor = _supervisorRespository.GetAll();
            var roles = _roleRepository.GetAll();

            return new Models.Dto.Output.InitCreateUser()
            {
                Companies = companies.ToList(),
                Centers = centers,
                Supervisors = supervisor,
                roles = roles
            };
        }

        public IEnumerable<User> ExecuteProcess(GetUserByPermissionSection getUserByPermissionSection)
        {
            var roles = _roleRepository.GetContextEntity().Include(r => r.Sections).ToList().Where((r) => (r.Sections != null && r.Sections.Any((s) => s.SectionsCode == SectionCode.PendingsAttendanceIncident)) || r.Code == RoleCode.Sudo);
                
            var roleIds = roles.Select(r => r.Id).ToList();
            var users = _repository.GetByFilter((user) => roleIds.Contains(user.RoleId));

            return users;
        }

        public IEnumerable<string> ExecuteProcess(GetTenantsUserByCompany getSectionsUserByCompany)
        {
            var findUser = _repository.GetById(Guid.Parse(getSectionsUserByCompany.UserId));
            var rol = _roleRepository.GetById(findUser!.RoleId);
            var findUserCompany = _userCompanyRepository.GetByFilter(uc => uc.UserId == Guid.Parse(getSectionsUserByCompany.UserId) && uc.CompanyId == getSectionsUserByCompany.CompanyId).FirstOrDefault();

            if (findUserCompany == null && rol!.Code != RoleCode.Sudo) {
                throw new BadHttpRequestException("El usuario no tiene acceso a esta empresa");
            }

            var tenants = new List<string>();

            if (rol!.Code == RoleCode.Sudo)
            {
                if (_globalPropertyService.TypeTenant == TypeTenant.Department)
                {
                    tenants = _centerRepository.GetContextEntity().Include(c => c.Keys).Where(c => c.Keys != null && c.Keys.Any()).Select(d => d.Id.Trim()).ToList();
                }
                else
                {
                    tenants = _supervisorRespository.GetAll().Select(s => s.Id.ToString()).ToList();
                }
            } else
            {
                if (_globalPropertyService.TypeTenant == TypeTenant.Department)
                {
                    tenants = _userDepartmentRespository.GetByFilter(d => d.UserCompanyId == findUserCompany!.Id).Select(d => d.DepartmentCode.Trim()).ToList();
                }
                else
                {
                    tenants = _userSupervisorRepository.GetByFilter(s => s.UserCompanyId == findUserCompany!.Id).Select(s => s.SupervisorId.ToString()).ToList();
                }
            }

            return tenants;
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

            return true;
        }
    }
}
