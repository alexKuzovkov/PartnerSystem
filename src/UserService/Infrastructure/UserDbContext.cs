using Microsoft.EntityFrameworkCore;
using UserService.Domain;

namespace UserService.Infrastructure;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.ExternalId)
                .IsUnique();

            entity.HasOne(e => e.Parent)
                .WithMany()
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}