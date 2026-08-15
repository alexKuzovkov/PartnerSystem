using CommissionService.Domain;
using CommissionService.Infrastructure;
using Microsoft.EntityFrameworkCore;
using PartnerSystem.Contracts;

namespace CommissionService.Application;

public sealed class SchemaSettingsService : ISchemaSettingsService
{
    private readonly CommissionDbContext _context;
    private readonly ILogger<SchemaSettingsService> _logger;

    public SchemaSettingsService(
        CommissionDbContext context,
        ILogger<SchemaSettingsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SchemaType> GetCurrentSchemaAsync(
        CancellationToken cancellationToken = default)
    {
        var settings = await _context.SchemaSettings
            .OrderByDescending(x => x.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is not null)
            return settings.CurrentSchema;

        settings = new SchemaSettings(SchemaType.Linear);
        _context.SchemaSettings.Add(settings);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created default commission scheme settings: {Schema}",
            settings.CurrentSchema);

        return settings.CurrentSchema;
    }

    public async Task SetCurrentSchemaAsync(
        SchemaType schemaType,
        CancellationToken cancellationToken = default)
    {
        var settings = await _context.SchemaSettings
            .OrderByDescending(x => x.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            settings = new SchemaSettings(schemaType);
            _context.SchemaSettings.Add(settings);
        }
        else
        {
            settings.ChangeSchema(schemaType, "admin");
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Commission scheme changed to {Schema}", schemaType);
    }
}
