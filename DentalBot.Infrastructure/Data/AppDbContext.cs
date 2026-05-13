using DentalBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DentalBot.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<PatientEmployee> PatientEmployees { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PatientEmployee>()
            .HasKey(pe => new { pe.PatientId, pe.EmployeeId });

            modelBuilder.Entity<PatientEmployee>()
                .HasOne(pe => pe.Patient)
                .WithMany(p => p.PatientEmployees)
                .HasForeignKey(pe => pe.PatientId);

            modelBuilder.Entity<PatientEmployee>()
                .HasOne(pe => pe.Employee)
                .WithMany(e => e.PatientEmployees)
                .HasForeignKey(pe => pe.EmployeeId);
        }
    }
}
