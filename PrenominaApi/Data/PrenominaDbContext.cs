using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;
using PrenominaApi.Extensions;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Models.Prenomina.Enums;
using System.Linq.Expressions;

namespace PrenominaApi.Data
{
    public class PrenominaDbContext : DbContext
    {
        private readonly TimeZoneInfo _timeZone;
        public PrenominaDbContext(
            DbContextOptions<PrenominaDbContext> options,
            TimeZoneInfo timeZone
        ) : base(options) {
            _timeZone = timeZone;
        }

        public DbSet<AssistanceIncident> assistanceIncidents { get; set; }
        public DbSet<AssistanceIncidentApprover> assistanceIncidentApprovers { get; set; }
        public DbSet<AuditLog> auditLogs { get; set; }
        public DbSet<EmployeeCheckIns> employeeCheckIns { get; set; }
        public DbSet<IncidentApprover> incidentApprovers { get; set; }
        public DbSet<IncidentCode> incidentCodes { get; set; }
        public DbSet<IncidentCodeMetadata> incidentCodeMetadata { get; set; }
        public DbSet<Role> roles { get; set; }
        public DbSet<Section> sections { get; set; }
        public DbSet<User> users { get; set; }
        public DbSet<UserCompany> userCompanies { get; set; }
        public DbSet<UserDepartment> userDepartments { get; set; }
        public DbSet<UserSupervisor> userSupervisors { get; set; }
        public DbSet<SystemConfig> systemConfigs { get; set; }
        public DbSet<KeyValue> keyValues { get; set; }
        public DbSet<IncidentOutputFile> incidentOutputFiles { get; set; }
        public DbSet<ColumnIncidentOutputFile> columnIncidentOutputFiles { get; set; }
        public DbSet<IgnoreIncidentToEmployee> ignoreIncidentToEmployees { get; set; }
        public DbSet<IgnoreIncidentToTenant> ignoreIncidentToTenants { get; set; }
        public DbSet<IgnoreIncidentToActivity> ignoreIncidentToActivities { get; set; }
        public DbSet<Period> periods { get; set; }
        public DbSet<Clock> clocks { get; set; }
        public DbSet<ClockUser> clockUsers { get; set; }
        public DbSet<ClockUserFinger> clockUserFingers { get; set; }
        public DbSet<ClockAttendance> clockAttendances { get; set; }
        public DbSet<DayOff> dayOffs { get; set; }
        public DbSet<RehiredEmployees> rehiredEmployees { get; set; }
        public DbSet<Document> documents { get; set; }
        public DbSet<SectionRol> sectionRols { get; set; }
        public DbSet<PeriodStatus> periodStatus { get; set; }
        public DbSet<WorkSchedule> workSchedules { get; set; }
        public DbSet<IncidentCodeAllowedRoles> incidentCodeAllowedRoles { get; set; }
        public DbSet<EmployeeAbsenceRequests> employeeAbsenceRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                // Excluir tipos compartidos o dinámicos
                if (entityType.ClrType == typeof(Dictionary<string, object>))
                {
                    continue;
                }

                var entity = modelBuilder.Entity(entityType.ClrType);

                var createdAtProperty = entityType.FindProperty("CreatedAt");
                if (createdAtProperty != null)
                {
                    entity.Property<DateTime>("CreatedAt").HasConversion(
                        v => v.ToUniversalTime(),
                        v => v.ToSpecificTimeZone(_timeZone)
                    );
                }

                var updatedAtProperty = entityType.FindProperty("UpdatedAt");
                if (updatedAtProperty != null)
                {
                    entity.Property<DateTime>("UpdatedAt").HasConversion(
                        v => v.ToUniversalTime(),
                        v => v.ToSpecificTimeZone(_timeZone)
                    );
                }

                var deletedAtProperty = entityType.FindProperty("DeletedAt");
                if (deletedAtProperty != null)
                {
                    entity.Property<DateTime?>("DeletedAt").HasConversion(
                        v => v.HasValue ? v.Value.ToUniversalTime() : (DateTime?)null,
                        v => v.HasValue ? v.Value.ToSpecificTimeZone(_timeZone) : (DateTime?)null
                    );

                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var propertyAccess = Expression.Property(parameter, "DeletedAt");
                    var nullValue = Expression.Constant(null, typeof(DateTime?));
                    var filter = Expression.Lambda(Expression.Equal(propertyAccess, nullValue), parameter);

                    entity.HasQueryFilter(filter);
                }
            }

            modelBuilder.Entity<AssistanceIncident>(entity =>
            {
                entity.HasOne(ai => ai.User).WithMany(u => u.AssistanceIncidents).HasForeignKey(ai => ai.ByUserId);
                entity.HasOne(ai => ai.ItemIncidentCode).WithMany(ic => ic.AssistanceIncidents).HasForeignKey(ai => ai.IncidentCode);
            });

            modelBuilder.Entity<AssistanceIncidentApprover>(entity =>
            {
                entity.HasOne(aip => aip.AssistanceIncident).WithMany(ai => ai.AssistanceIncidentApprover).HasForeignKey(aip => aip.AssistanceIncidentId);
                entity.HasOne(aip => aip.IncidentApprover).WithMany(ia => ia.AssistanceIncidentApprover).HasForeignKey(aip => aip.IncidentApproverId).OnDelete(DeleteBehavior.Restrict); ;
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasOne(al => al.User).WithMany(u => u.AuditLogs).HasForeignKey(al => al.ByUserId);
            });

            modelBuilder.Entity<IncidentApprover>(entity =>
            {
                entity.HasOne(ia => ia.ItemIncidentCode).WithMany(ic => ic.IncidentApprovers).HasForeignKey(ia => ia.IncidentCode);
                entity.HasOne(ia => ia.User).WithMany(u => u.IncidentApprovers).HasForeignKey(ia => ia.UserId);
            });

            modelBuilder.Entity<IncidentCode>(entity =>
            {
                entity.HasOne(ic => ic.IncidentCodeMetadata).WithOne(icm => icm.IncidentCode).HasForeignKey<IncidentCode>(ic => ic.MetadataId);
                entity.HasMany(ic => ic.IncidentCodeAllowedRoles).WithOne(icar => icar.ItemIncidentCode).HasForeignKey(icr => icr.IncidentCode);
            });

            modelBuilder.Entity<SectionRol>(entity =>
            {
                entity.HasKey(sr => new { sr.SectionsCode, sr.RolesId });
                entity.HasOne(sr => sr.Section).WithMany(s => s.Roles).HasForeignKey(sr => sr.SectionsCode);
                entity.HasOne(sr => sr.Role).WithMany(r => r.Sections).HasForeignKey(sr => sr.RolesId);
                entity.Property(sr => sr.PermissionsJson).HasColumnType("nvarchar(max)");
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasMany(r => r.Sections).WithOne(sr => sr.Role).HasForeignKey(sr => sr.RolesId);
                entity.HasMany(r => r.Users).WithOne(u => u.Role).HasForeignKey(u => u.RoleId);
            });

            modelBuilder.Entity<Section>(entity =>
            {
                entity.HasMany(u => u.Roles).WithOne(r => r.Section).HasForeignKey(r => r.SectionsCode);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasOne(u => u.Role).WithMany(r => r.Users).HasForeignKey(u => u.RoleId);
                entity.HasIndex(u => u.Email).IsUnique();
            });

            modelBuilder.Entity<KeyValue>(entity =>
            {
                entity.HasIndex(k => k.Code).IsUnique();
            });

            modelBuilder.Entity<ColumnIncidentOutputFile>(entity =>
            {
                entity.HasOne(c => c.KeyValue).WithMany(c => c.ColumnIncidentOutputFiles).HasForeignKey(c => c.KeyValueId);
            });

            modelBuilder.Entity<DayOff>(entity =>
            {
                entity.HasOne(d => d.IncidentCodeItem).WithMany(i => i.DayOffs).HasForeignKey(d => d.IncidentCode);
                entity.HasIndex(d => d.Date).IsUnique();
            });

            modelBuilder.Entity<UserCompany>(entity =>
            {
                entity.HasOne(uc => uc.User).WithMany(u => u.Companies).HasForeignKey(uc => uc.UserId);
                entity.HasMany(uc => uc.UserDepartments).WithOne(ud => ud.UserCompany).HasForeignKey(uc => uc.UserCompanyId);
                entity.HasMany(uc => uc.UserSupervisors).WithOne(us => us.UserCompany).HasForeignKey(uc => uc.UserCompanyId);
            });

            modelBuilder.Entity<SystemConfig>(entity =>
            {
                entity.HasIndex(sc => sc.Key).IsUnique();
            });

            modelBuilder.Entity<IgnoreIncidentToEmployee>(entity =>
            {
                entity.HasOne(i => i.IncidentCodeItem).WithMany(c => c.IgnoreIncidentToEmployees).HasForeignKey(i => i.IncidentCode);
            });

            modelBuilder.Entity<IgnoreIncidentToTenant>(entity =>
            {
                entity.HasOne(i => i.IncidentCodeItem).WithMany(c => c.IgnoreIncidentToTenants).HasForeignKey(i => i.IncidentCode);
            });

            modelBuilder.Entity<IgnoreIncidentToActivity>(entity =>
            {
                entity.HasOne(i => i.IncidentCodeItem).WithMany(c => c.IgnoreIncidentToActivities).HasForeignKey(i => i.IncidentCode);
            });

            modelBuilder.Entity<ClockUser>(entity =>
            {
                entity.HasIndex(cu => cu.EnrollNumber).IsUnique();
                entity.HasMany(cu => cu.UserFingers).WithOne(uf => uf.ClockUser).HasForeignKey(uf => uf.EnrollNumber).HasPrincipalKey(cu => cu.EnrollNumber);
            });

            modelBuilder.Entity<Clock>(entity =>
            {
                entity.HasIndex(c => c.Ip).IsUnique();
            });

            modelBuilder.Entity<RehiredEmployees>(entity =>
            {
                entity.Property(e => e.Observation).HasColumnName("observation").HasMaxLength(1500);
            });

            modelBuilder.Entity<IncidentCodeAllowedRoles>(entity =>
            {
                entity.HasKey(e => new { e.IncidentCode, e.RoleId });
                entity.Property(e => e.IncidentCode).IsRequired();
                entity.Property(e => e.RoleId).IsRequired();
                entity.HasOne<IncidentCode>().WithMany().HasForeignKey(e => e.IncidentCode);
                entity.HasOne<Role>().WithMany().HasForeignKey(e => e.RoleId).HasPrincipalKey(r => r.Id);
            });

            modelBuilder.Entity<EmployeeAbsenceRequests>(entity =>
            {
                entity.Property(e => e.IncidentCode).IsRequired();
                entity.Property(e => e.EmployeeCode).IsRequired();
                entity.Property(e => e.CompanyId).IsRequired();
                entity.HasOne(e => e.IncidentCodeItem).WithMany(ic => ic.EmployeeAbsenceRequests).HasForeignKey(e => e.IncidentCode);
            });

            var converter = new ValueConverter<IEnumerable<string>, string>(
                v => JsonConvert.SerializeObject(v), // Guardar como JSON
                v => JsonConvert.DeserializeObject<IEnumerable<string>>(v) ?? Enumerable.Empty<string>() // Leer desde JSON
            );

            modelBuilder.Entity<Document>()
                .Property(e => e.KeyParams)
                .HasConversion(converter)
                .HasColumnType("nvarchar(max)");

            base.OnModelCreating(modelBuilder);
        }

        public override int SaveChanges()
        {
            foreach (var entity in ChangeTracker.Entries()) {
                var entityType = entity.Entity.GetType();
                var deletedAtProperty = entityType.GetProperty("DeletedAt");

                if (entity.State == EntityState.Deleted && deletedAtProperty != null && deletedAtProperty.PropertyType == typeof(DateTime))
                {
                    entity.State = EntityState.Modified;
                    deletedAtProperty.SetValue(entity.Entity, DateTime.UtcNow);
                }
            }

            return base.SaveChanges();
        }

        public static void Seed(PrenominaDbContext context)
        {
            if (!context.systemConfigs.Any())
            {
                context.systemConfigs.Add(new SystemConfig()
                {
                    Key = "Year-Operation",
                    Data = JsonConvert.SerializeObject(new SysYearOperation()
                    {
                        TypeData = "Int",
                        Year = DateTime.Now.Year,
                    }),
                });

                context.SaveChanges();
            }

            if (!context.systemConfigs.Where(s => s.Key == "Type-Tenant").Any())
            {
                context.systemConfigs.Add(new SystemConfig()
                {
                    Key = "Type-Tenant",
                    Data = JsonConvert.SerializeObject(new SysTypeTenant()
                    {
                        TypeTenant = TypeTenant.Department,
                    })
                });

                context.SaveChanges();
            }

            if (!context.systemConfigs.Where(s => s.Key == "Absenteeism-Factor").Any())
            {
                context.systemConfigs.Add(new SystemConfig()
                {
                    Key = "Absenteeism-Factor",
                    Data = JsonConvert.SerializeObject(new SysAbsenteeismFactor()
                    {
                        Factor = 1,
                    })
                });

                context.SaveChanges();
            }

            if (!context.systemConfigs.Where(s => s.Key == SysConfig.ExtractChecks).Any())
            {
                context.systemConfigs.Add(new SystemConfig()
                {
                    Key = SysConfig.ExtractChecks,
                    Data = JsonConvert.SerializeObject(new SysExtractCheck()
                    {
                        IntervalInMinutes = 30,
                    })
                });

                context.SaveChanges();
            }

            if (!context.incidentCodes.Any())
            {
                context.incidentCodes.AddRange([
                    new IncidentCode() {
                        Code = DefaultIncidentCodes.Falta,
                        ExternalCode = "F",
                        Label = "Falta",
                    },
                    new IncidentCode() {
                        Code = DefaultIncidentCodes.PermisoSinGoce,
                        ExternalCode = "P",
                        Label = "Permiso Sin Goce",
                    },
                    new IncidentCode() {
                        Code = DefaultIncidentCodes.Castigo,
                        ExternalCode = "S",
                        Label = "Castigo",
                    },
                    new IncidentCode() {
                        Code = DefaultIncidentCodes.ParoTecnico,
                        ExternalCode = "T",
                        Label = "Paro Tecnico",
                    },
                ]);

                context.SaveChanges();
            }

            var existICPrimaDominical = context.incidentCodes.Find(DefaultIncidentCodes.PrimaDominical);

            if (existICPrimaDominical == null)
            {

                var metaPDOM = new IncidentCodeMetadata()
                {
                    Amount = 0.25M,
                    MathOperation = MathOperation.Multiplication,
                    ColumnForOperation = ColumnForOperation.Salary,
                };

                context.incidentCodeMetadata.Add(metaPDOM);

                var icPDOM = context.incidentCodes.Add(new IncidentCode()
                {
                    Code = DefaultIncidentCodes.PrimaDominical,
                    ExternalCode = "9",
                    Label = "Prima Dominical",
                    WithOperation = true,
                    IsAdditional = true,
                    ApplyMode = IncidentCodeApplyMode.Day,
                    MetadataId = metaPDOM.Id
                });

                context.SaveChanges();
            }

            var existICDayOff = context.incidentCodes.Find(DefaultIncidentCodes.DescansoLaborado);

            if (existICPrimaDominical == null)
            {
                var metaDOFF = new IncidentCodeMetadata()
                {
                    Amount = 2,
                    MathOperation = MathOperation.Multiplication,
                    ColumnForOperation = ColumnForOperation.Salary,
                };

                context.incidentCodeMetadata.Add(metaDOFF);

                context.incidentCodes.Add(new IncidentCode()
                {
                    Code = DefaultIncidentCodes.DescansoLaborado,
                    ExternalCode = "10",
                    Label = "Descanso Laborado",
                    WithOperation = true,
                    IsAdditional = true,
                    ApplyMode = IncidentCodeApplyMode.Day,
                    MetadataId = metaDOFF.Id
                });

                context.SaveChanges();
            }

            var existDobleTurno = context.incidentCodes.Find(DefaultIncidentCodes.DobleTurno);

            if (existDobleTurno == null)
            {
                var metaDT = new IncidentCodeMetadata()
                {
                    Amount = 2,
                    MathOperation = MathOperation.Multiplication,
                    ColumnForOperation = ColumnForOperation.Salary,
                };

                context.incidentCodeMetadata.Add(metaDT);

                context.incidentCodes.Add(new IncidentCode()
                {
                    Code = DefaultIncidentCodes.DobleTurno,
                    ExternalCode = DefaultIncidentCodes.DobleTurno,
                    Label = "Doble Turno",
                    WithOperation = true,
                    IsAdditional = true,
                    ApplyMode = IncidentCodeApplyMode.Day,
                    MetadataId = metaDT.Id
                });

                context.SaveChanges();
            }


            if (!context.roles.Any())
            {
                context.roles.Add(new Role() {
                    Code = RoleCode.Sudo,
                    Label = "System"
                });

                context.SaveChanges();
            }

            if (context.roles.Any() && !context.users.Any())
            {
                var rol = context.roles.Where((item) => item.Code == RoleCode.Sudo).First();

                context.users.Add(new User()
                {
                    Email = "system@prenominaapi.com",
                    Password = "59FbTmppFhZswRSpv2TxUA==:57cO2NgkZLBVjUJ793ekVY5QlfvGuDjGt+NzjCUMU8k=",
                    RoleId = rol.Id,
                    Name = "System Admin"
                });

                context.SaveChanges();
            }

            if (!context.keyValues.Any())
            {
                context.keyValues.AddRange([
                    new KeyValue() {
                        Code = "employe:codigo",
                        Label = "Código de empleado",
                    },
                    new KeyValue() {
                        Code = "employe:name",
                        Label = "Nombre de empleado",
                    },
                    new KeyValue() {
                        Code = "employe:salary",
                        Label = "Sueldo de empleado",
                    },
                    new KeyValue() {
                        Code = "incident:date",
                        Label = "Fecha de incidencia",
                    },
                    new KeyValue() {
                        Code = "incident:value",
                        Label = "Valor de incidencia",
                    },
                    new KeyValue() {
                        Code = "incident:code",
                        Label = "Código de incidencia",
                    },
                    new KeyValue() {
                        Code = "incident:external-code",
                        Label = "Código externo del incidencia",
                    },
                    new KeyValue() {
                        Code = "sys:absenteeism-factor",
                        Label = "Factor de ausentismo",
                    },
                ]);

                context.SaveChanges();
            }

            if (!context.dayOffs.Any())
            {
                context.dayOffs.AddRange([
                    new DayOff() {
                        Date = new DateOnly(1993, 8, 1),
                        Description = "Prima Dominical",
                        IncidentCode = DefaultIncidentCodes.PrimaDominical,
                        IsSunday = true,
                    },
                    new DayOff() {
                        Date = new DateOnly(1993, 1, 1),
                        Description = "Año Nuevo",
                        IncidentCode = DefaultIncidentCodes.DescansoLaborado,
                        IsSunday = false,
                    },
                    new DayOff() {
                        Date = new DateOnly(1993, 2, 3),
                        Description = "Día de la Constitución",
                        IncidentCode = DefaultIncidentCodes.DescansoLaborado,
                        IsSunday = false,
                    },
                    new DayOff() {
                        Date = new DateOnly(1993, 3, 17),
                        Description = "Natalicio de Benito Juárez",
                        IncidentCode = DefaultIncidentCodes.DescansoLaborado,
                        IsSunday = false,
                    },
                    new DayOff() {
                        Date = new DateOnly(1993, 5, 1),
                        Description = "Día del Trabajo",
                        IncidentCode = DefaultIncidentCodes.DescansoLaborado,
                        IsSunday = false,
                    },
                    new DayOff() {
                        Date = new DateOnly(1993, 9, 16),
                        Description = "Aniversario de la Independencia",
                        IncidentCode = DefaultIncidentCodes.DescansoLaborado,
                        IsSunday = false,
                    },
                    new DayOff() {
                        Date = new DateOnly(1993, 11, 17),
                        Description = "Revolución Mexicana",
                        IncidentCode = DefaultIncidentCodes.DescansoLaborado,
                        IsSunday = false,
                    },
                    new DayOff() {
                        Date = new DateOnly(1993, 12, 25),
                        Description = "Navidad",
                        IncidentCode = DefaultIncidentCodes.DescansoLaborado,
                        IsSunday = false,
                    }
                ]);

                context.SaveChanges();
            }

            var existVacation = context.incidentCodes.Find(DefaultIncidentCodes.VAC);

            if (existVacation == null)
            {
                context.incidentCodes.AddRange([
                    new IncidentCode() {
                        Code = DefaultIncidentCodes.EG,
                        ExternalCode = "109",
                        Label = "Incapacidad por enf. gral.",
                    },
                    new IncidentCode() {
                        Code = DefaultIncidentCodes.MT,
                        ExternalCode = "110",
                        Label = "Incapacidad por maternidad",
                    },
                    new IncidentCode() {
                        Code = DefaultIncidentCodes.RT,
                        ExternalCode = "111",
                        Label = "Incapacidad por riesgo de trabajo",
                    },
                    new IncidentCode() {
                        Code = DefaultIncidentCodes.VAC,
                        ExternalCode = "30",
                        Label = "Vacaciones",
                    },
                ]);

                context.SaveChanges();
            }

            if (!context.systemConfigs.Where(s => s.Key == SysConfig.ConfigReports).Any())
            {
                context.systemConfigs.Add(new SystemConfig()
                {
                    Key = SysConfig.ConfigReports,
                    Data = JsonConvert.SerializeObject(new SysConfigReports()
                    {
                        ConfigDayOffReport = new ConfigDayOffReport()
                        {
                            TypeDayOffReport = TypeDayOffReport.Default
                        }
                    })
                });

                context.SaveChanges();
            }
        }
    }
}
