using Microsoft.EntityFrameworkCore;

namespace CommissionService.Infrastructure;

public class CommissionDbContext : DbContext
{
    public CommissionDbContext(DbContextOptions<CommissionDbContext> options)
        : base(options)
    {
    }

    public DbSet<Domain.Commission> Commissions => Set<Domain.Commission>();

    public DbSet<SchemaSettings> SchemaSettings { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SchemaSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<Domain.Commission>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.EventExternalId, e.PartnerExternalId, e.Level })
                .IsUnique();

            entity.HasIndex(e => e.PartnerExternalId);
            entity.HasIndex(e => e.IsPaid);

            entity.Property(e => e.SchemaType)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18,2)");
        });
    }
}