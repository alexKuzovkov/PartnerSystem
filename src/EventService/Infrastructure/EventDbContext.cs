using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EventService.Infrastructure;

public class EventDbContext : DbContext
{
    public EventDbContext(DbContextOptions<EventDbContext> options)
        : base(options)
    {
    }

    public DbSet<Domain.ProfitEvent> ProfitEvents => Set<Domain.ProfitEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Domain.ProfitEvent>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.EventExternalId)
                .IsUnique();

            entity.HasIndex(e => new { e.UserExternalId, e.OccurredAt });
        });

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}