using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EventService.Infrastructure.Outbox;

public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly TimeSpan _interval;

    public OutboxProcessor(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessor> logger,
        TimeSpan? interval = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _interval = interval ?? TimeSpan.FromSeconds(5);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("OutboxProcessor запущен. Интервал: {Interval}", _interval);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке Outbox");
            }

            await Task.Delay(_interval, cancellationToken);
        }

        _logger.LogInformation("OutboxProcessor остановлен");
    }

    private async Task ProcessOutboxAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EventDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var messages = await context.OutboxMessages
            .Where(m => m.ProcessedDate == null)
            .OrderBy(m => m.OccurredOn)
            .Take(50)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
            return;

        _logger.LogDebug("Найдено {Count} сообщений для отправки", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                var eventType = Type.GetType(message.Type);
                if (eventType == null)
                {
                    _logger.LogError("Не найден тип события {Type}", message.Type);
                    continue;
                }

                var eventData = JsonSerializer.Deserialize(message.Data, eventType);
                if (eventData == null)
                {
                    _logger.LogError("Не удалось десериализовать событие {Type}", message.Type);
                    continue;
                }

                await publisher.Publish(eventData, cancellationToken);

                message.MarkAsProcessed();

                _logger.LogDebug("Сообщение {Id} типа {Type} опубликовано",
                    message.Id, message.Type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при публикации сообщения {Id}", message.Id);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}