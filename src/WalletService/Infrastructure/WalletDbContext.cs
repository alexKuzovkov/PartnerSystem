using Microsoft.EntityFrameworkCore;
using WalletService.Domain;

namespace WalletService.Infrastructure;

public class WalletDbContext : DbContext
{
    public WalletDbContext(DbContextOptions<WalletDbContext> options)
        : base(options)
    {
    }

    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletTransaction> Transactions => Set<WalletTransaction>();
    public DbSet<PendingPayout> PendingPayouts => Set<PendingPayout>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.UserExternalId)
                .IsUnique();

            entity.Property(e => e.Balance)
                .HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<WalletTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.UserExternalId, e.CommissionEventExternalId })
                .IsUnique();

            entity.HasIndex(e => e.UserExternalId);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<PendingPayout>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.IsPaid, e.LockedAt });
            entity.HasIndex(e => new { e.CommissionEventExternalId, e.PartnerExternalId, e.Level })
                .IsUnique();
        });
    }
}