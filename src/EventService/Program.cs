using EventService.Application;
using EventService.Infrastructure;
using EventService.Infrastructure.Outbox;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddDbContext<EventDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "database");

var rabbitMqHost = builder.Configuration["RabbitMQ:HostName"];
if (!string.IsNullOrWhiteSpace(rabbitMqHost))
{
    var rabbitMqUserName = builder.Configuration["RabbitMQ:UserName"] ?? "guest";
    var rabbitMqPassword = builder.Configuration["RabbitMQ:Password"] ?? "guest";
}

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<ProfitEventConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:HostName"]!, h =>
        {
            h.Username(builder.Configuration["RabbitMQ:UserName"]!);
            h.Password(builder.Configuration["RabbitMQ:Password"]!);
        });

        cfg.ReceiveEndpoint("event-service", e =>
        {
            e.ConfigureConsumer<ProfitEventConsumer>(context);

            e.ConcurrentMessageLimit = 16; 
            e.PrefetchCount = 16;        

            e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));

            e.UseCircuitBreaker(cb =>
            {
                cb.TrackingPeriod = TimeSpan.FromMinutes(1);
                cb.TripThreshold = 15;
                cb.ActiveThreshold = 5;
                cb.ResetInterval = TimeSpan.FromMinutes(5);
            });
        });

        cfg.ConfigureEndpoints(context);
    });

    x.AddEntityFrameworkOutbox<EventDbContext>(entityFrameworkOutboxOptions =>
    {
        entityFrameworkOutboxOptions.UsePostgres();
        entityFrameworkOutboxOptions.UseBusOutbox();
        entityFrameworkOutboxOptions.DuplicateDetectionWindow = TimeSpan.FromSeconds(30);
    });
});

builder.Services.AddHostedService<OutboxProcessor>();

builder.Services.AddScoped<IEventProcessor, EventProcessor>();
builder.Services.AddScoped<IEventService, EventService.Application.EventService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<EventDbContext>();
    dbContext.Database.Migrate();
}

app.MapHealthChecks("/health");
app.MapControllers();

var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() => Log.Information("Graceful shutdown..."));
lifetime.ApplicationStopped.Register(() =>
{
    Log.Information("Stopped");
    Log.CloseAndFlush();
});

Log.Information("EventService started");
app.Run();