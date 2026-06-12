using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace WalletService.Infrastructure;

public class WalletDbContextFactory : IDesignTimeDbContextFactory<WalletDbContext>
{
    public WalletDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var builder = new DbContextOptionsBuilder<WalletDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        builder.UseNpgsql(connectionString, b => b.MigrationsAssembly("WalletService"));

        return new WalletDbContext(builder.Options);
    }
}