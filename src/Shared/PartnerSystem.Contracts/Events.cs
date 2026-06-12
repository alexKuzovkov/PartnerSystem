namespace PartnerSystem.Contracts;

// <summary>
/// Событие о прибыли/убытке пользователя
/// </summary>
public record ProfitEvent(
    string EventExternalId,
    string UserExternalId,
    decimal Profit,
    DateTime OccurredAt
);

/// <summary>
/// Запрос на расчет комиссий по событию
/// </summary>
public record CommissionCalculationRequested(
    string EventExternalId,
    string UserExternalId,
    decimal Profit,
    DateTime OccurredAt);

/// <summary>
/// Тип схемы расчета комиссий
/// </summary>
public enum SchemaType
{
    Linear = 0,
    Fibonacci = 1
}

/// <summary>
/// Результат расчета комиссии для одного уровня
/// </summary>
public record CommissionCalculated(
    string EventExternalId,
    string PartnerExternalId,
    int Level,
    decimal Amount,
    SchemaType SchemaType
);