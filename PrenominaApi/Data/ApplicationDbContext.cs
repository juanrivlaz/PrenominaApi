using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PrenominaApi.Models;

namespace PrenominaApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Payroll> Payrolls { get; set; }
        public DbSet<Period> Period { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Center> Centers { get; set; }
        public DbSet<Supervisor> Supervisors { get; set; }
        public DbSet<AttendanceRecords> AttendanceRecords { get; set; }
        public DbSet<Key> Keys { get; set; }
        public DbSet<Tabulator> Tabulators { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<Kardex> Kardex { get; set; }
        public DbSet<Vacations> Vacations { get; set; }
        public DbSet<Deduction> Deductions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var dateOnlyConverter = new ValueConverter<DateOnly, DateTime>(
                dateOnly => dateOnly.ToDateTime(TimeOnly.MinValue),
                dateTime => DateOnly.FromDateTime(dateTime)
            );

            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => new { e.Company, e.Codigo }).HasName("PK36");
                entity.Property(property => property.Codigo).HasColumnType("numeric(8,0)");
                entity.Property(property => property.Company).HasColumnType("numeric(3,0)");

                entity.HasMany(e => e.Keys).WithMany(k => k.Employees);
            });

            modelBuilder.Entity<AttendanceRecords>(entity =>
            {
                entity.HasNoKey();
                entity.Property(property => property.Company).HasColumnType("numeric(18,0)");
                entity.Property(property => property.Codigo).HasColumnType("numeric(18,0)");
                entity.Property(property => property.Date).HasConversion(dateOnlyConverter);
            });

            modelBuilder.Entity<Payroll>(entity =>
            {
                entity.HasKey(e => new { e.Company, e.TypeNom }).HasName("PK110");
                entity.Property(property => property.Company).HasColumnType("numeric(3,0)");
            });

            modelBuilder.Entity<Period>(entity =>
            {
                entity.HasKey(e => new { e.TypeNom, e.Number, e.YearOfOperation, e.Company }).HasName("PK107");
                entity.Property(property => property.Company).HasColumnType("numeric(3,0)");
                entity.Property(property => property.Days).HasColumnType("decimal(6,2)");
            });

            modelBuilder.Entity<Company>(entity =>
            {
                entity.Property(property => property.Id).HasColumnType("numeric(3,0)");
            });

            modelBuilder.Entity<Center>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.Company }).HasName("PK26");
                entity.Property(property => property.Company).HasColumnType("numeric(3,0)");
                entity.HasMany(c => c.Keys).WithOne(k => k.CenterItem).HasForeignKey(c => new { c.Center, c.Company } );
            });

            modelBuilder.Entity<Supervisor>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.Company }).HasName("PK98");
                entity.Property(property => property.Id).HasColumnType("numeric(5,0)");
                entity.Property(property => property.Company).HasColumnType("numeric(3,0)");
            });

            modelBuilder.Entity<Key>(entity =>
            {
                entity.HasKey(e => new {
                    e.Company,
                    e.Codigo,
                    e.Center,
                    e.Clase,
                    e.Supervisor,
                    e.Ocupation,
                    e.Schedule,
                    e.TypeNom,
                    e.Bank,
                }).HasName("PK106");

                entity.Property(property => property.Company).HasColumnType("numeric(3,0)");
                entity.Property(property => property.Codigo).HasColumnType("numeric(8,0)");
                entity.Property(property => property.Supervisor).HasColumnType("numeric(5,0)");
                entity.HasOne(k => k.Tabulator).WithMany(t => t.Keys).HasForeignKey(k => new { k.Company, k.Ocupation });
                entity.HasOne(k => k.SupervisorItem).WithMany(s => s.Keys).HasForeignKey(k => new { k.Supervisor, k.Company }).HasPrincipalKey(s => new { s.Id, s.Company });
                entity.HasOne(k => k.Employee).WithOne(e => e.Key).HasForeignKey<Key>(k => new { k.Company, k.Codigo }).HasPrincipalKey<Employee>(e => new { e.Company, e.Codigo });
            });

            modelBuilder.Entity<Tabulator>(entity =>
            {
                entity.HasKey(e => new
                {
                    e.Company,
                    e.Ocupation
                }).HasName("PK100");
                entity.Property(property => property.Company).HasColumnType("numeric(3,0)");
            });

            modelBuilder.Entity<Contract>(entity =>
            {
                entity.HasKey(e => new
                {
                    e.Codigo,
                    e.Folio,
                    e.Company,
                }).HasName("PK28");
                entity.Property(property => property.Company).HasColumnType("numeric(3,0)");
                entity.Property(property => property.Codigo).HasColumnType("numeric(8,0)");
                entity.Property(property => property.Salary).HasColumnType("decimal(12,2)");
            });

            modelBuilder.Entity<Kardex>(entity =>
            {
                entity.HasKey(e => new
                {
                    e.Codigo,
                    e.Company,
                    e.Paysheet,
                    e.Centro,
                    e.NumConc,
                    e.Class,
                    e.Folio,
                }).HasName("PK55");
                entity.Property(property => property.Codigo).HasColumnType("numeric(8,0)");
                entity.Property(property => property.Company).HasColumnType("numeric(3,0)");
                entity.Property(property => property.Days).HasColumnType("decimal(6,2)");
            });

            modelBuilder.Entity<Vacations>(entity =>
            {
                entity.HasKey(e => new
                {
                    e.Codigo,
                    e.Company,
                }).HasName("PK103");
                entity.Property(property => property.Codigo).HasColumnType("numeric(8,0)");
                entity.Property(property => property.Company).HasColumnType("numeric(3,0)");
            });

            modelBuilder.Entity<Deduction>(entity =>
            {
                entity.HasKey(e => new
                {
                    e.Company,
                    e.Codigo,
                    e.Folio,
                    e.NumConc,
                    e.Centro,
                    e.Clase,
                    e.MonthOperation,
                    e.YearOperation,
                    e.TypeNom,
                    e.Period,
                }).HasName("PK30");
                entity.Property(property => property.Codigo).HasColumnType("numeric(8,0)");
                entity.Property(property => property.Company).HasColumnType("numeric(3,0)");
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
