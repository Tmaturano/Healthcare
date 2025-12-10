using Healthcare.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Healthcare.Infrastructure;

public class HealthcareDbContext : DbContext
{
    public HealthcareDbContext(DbContextOptions<HealthcareDbContext> options)
    : base(options) { }

    public DbSet<Patient> Patients { get; set; }
    public DbSet<UsageEvent> UsageEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UsageEvent>()
            .HasKey(e => e.Id);

        modelBuilder.Entity<UsageEvent>()
            .HasIndex(e => e.ExternalEventId)
            .IsUnique();
    }
}
