using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EventService.Infrastructure;

public class EventDbContextFactory : IDesignTimeDbContextFactory<EventDbContext>
{
    public EventDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var builder = new DbContextOptionsBuilder<EventDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        builder.UseNpgsql(connectionString, b => b.MigrationsAssembly("EventService"));

        return new EventDbContext(builder.Options);
    }
}