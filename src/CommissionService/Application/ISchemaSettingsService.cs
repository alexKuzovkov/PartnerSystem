using PartnerSystem.Contracts;

namespace CommissionService.Application;

public interface ISchemaSettingsService
{
    Task<SchemaType> GetCurrentSchemaAsync();
    Task SetCurrentSchemaAsync(SchemaType schemaType, CancellationToken cancellationToken);
}