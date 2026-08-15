using PartnerSystem.Contracts;

namespace CommissionService.Application;

public interface ISchemaSettingsService
{
    Task<SchemaType> GetCurrentSchemaAsync(CancellationToken cancellationToken = default);

    Task SetCurrentSchemaAsync(
        SchemaType schemaType,
        CancellationToken cancellationToken = default);
}
