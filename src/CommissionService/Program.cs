using System.Net;
using CommissionService.Application;
using CommissionService.Consumers;
using CommissionService.Infrastructure;
using Grpc.Core;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using PartnerSystem.Contracts.Grpc;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using Serilog;
using StackExchange.Redis;

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

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var connectionString = builder.Configuration["Redis:ConnectionString"]!;
    return ConnectionMultiplexer.Connect(connectionString);
});

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
    options.InstanceName = "PartnerSystem:";
});

builder.Services.AddSingleton<IDistributedLockFactory>(sp =>
{
    var connectionString = builder.Configuration["Redis:ConnectionString"]!;

    var config = ConfigurationOptions.Parse(connectionString);

    var endPoints = config.EndPoints
        .Select(ep => new RedLockEndPoint { EndPoint = ep })
        .ToList();

    return RedLockFactory.Create(endPoints);
});

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<CommissionCalculationConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:HostName"]!, h =>
        {
            h.Username(builder.Configuration["RabbitMQ:UserName"]!);
            h.Password(builder.Configuration["RabbitMQ:Password"]!);
        });
        cfg.ReceiveEndpoint("commission-service", e =>
        {
            e.ConfigureConsumer<CommissionCalculationConsumer>(context);
            e.ConcurrentMessageLimit = 16;
            e.PrefetchCount = 16;
            e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
        });

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddGrpcClient<PartnerService.PartnerServiceClient>(o =>
{
    o.Address = new Uri(builder.Configuration["UserService:GrpcAddress"]!);
})
.ConfigureChannel(channel =>
{
    channel.Credentials = ChannelCredentials.Insecure;
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