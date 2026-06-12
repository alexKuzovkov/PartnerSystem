using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CommissionService.Infrastructure;

public class CommissionDbContextFactory : IDesignTimeDbContextFactory<CommissionDbContext>
{
    public CommissionDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var builder = new DbContextOptionsBuilder<CommissionDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        builder.UseNpgsql(connectionString, b => b.MigrationsAssembly("CommissionService"));

        return new CommissionDbContext(builder.Options);
    }
}