using CommissionService.Application;
using CommissionService.Consumers;
using CommissionService.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using PartnerSystem.Contracts.Grpc;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddDbContext<CommissionDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "database")
    .AddRedis(builder.Configuration["Redis:ConnectionString"]!, name: "redis");

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
    options.InstanceName = "PartnerSystem:";
});

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<CommissionCalculationConsumer>();

    x.AddEntityFrameworkOutbox<CommissionDbContext>(options =>
    {
        options.UsePostgres();
        options.UseBusOutbox();
        options.DuplicateDetectionWindow = TimeSpan.FromMinutes(5);
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:HostName"]!, h =>
        {
            h.Username(builder.Configuration["RabbitMQ:UserName"]!);
            h.Password(builder.Configuration["RabbitMQ:Password"]!);
        });
        cfg.ReceiveEndpoint("commission-service", e =>
        {
            e.ConcurrentMessageLimit = 16;
            e.PrefetchCount = 16;
            e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
            e.UseEntityFrameworkOutbox<CommissionDbContext>(context);
            e.ConfigureConsumer<CommissionCalculationConsumer>(context);
        });
    });
});

builder.Services.AddGrpcClient<PartnerService.PartnerServiceClient>(o =>
{
    o.Address = new Uri(builder.Configuration["UserService:GrpcAddress"]!);
});

builder.Services.AddSingleton<ICommissionCalculator, CommissionCalculator>();
builder.Services.AddScoped<ICommissionService, CommissionService.Application.CommissionService>();
builder.Services.AddScoped<ISchemaSettingsService, SchemaSettingsService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

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
    var dbContext = scope.ServiceProvider.GetRequiredService<CommissionDbContext>();
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

Log.Information("CommissionService started");
app.Run();