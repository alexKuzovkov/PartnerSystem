using CommissionService.Infrastructure;
using Microsoft.EntityFrameworkCore;
using PartnerSystem.Contracts;

namespace CommissionService.Application;

public class SchemaSettingsService : ISchemaSettingsService
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

    public async Task<SchemaType> GetCurrentSchemaAsync()
    {
        var settings = await _context.SchemaSettings.FirstOrDefaultAsync();

        if (settings == null)
        {
            settings = new SchemaSettings(SchemaType.Linear);
            _context.SchemaSettings.Add(settings);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Созданы настройки схемы по умолчанию: {Schema}", settings.CurrentSchema);
        }

        return settings.CurrentSchema;
    }

    public async Task SetCurrentSchemaAsync(SchemaType schemaType, CancellationToken cancellationToken)
    {
        var settings = await _context.SchemaSettings.FirstOrDefaultAsync();

        if (settings == null)
        {
            settings = new SchemaSettings(schemaType);
            _context.SchemaSettings.Add(settings);
        }
        else
        {
            settings.ChangeSchema(schemaType, "admin");
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Схема переключена на {Schema}", schemaType);
    }
}