using Microsoft.EntityFrameworkCore;
using Serilog;
using WalletService.Infrastructure;
using WalletService.Application;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddDbContext<WalletDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "database");

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<PayoutConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:HostName"]!, h =>
        {
            h.Username(builder.Configuration["RabbitMQ:UserName"]!);
            h.Password(builder.Configuration["RabbitMQ:Password"]!);
        });

        cfg.ReceiveEndpoint("wallet-service-commission-calculated", e =>
        {
            e.ConcurrentMessageLimit = 16;
            e.PrefetchCount = 16;
            e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
            e.ConfigureConsumer<PayoutConsumer>(context);
        });
    });
});


var payoutInterval = TimeSpan.FromSeconds(
    builder.Configuration.GetValue<int>("PayoutSettings:IntervalSeconds", 30));

builder.Services.AddHostedService(sp =>
    new PayoutBackgroundService(
        sp,
        sp.GetRequiredService<ILogger<PayoutBackgroundService>>(),
        payoutInterval));

builder.Services.AddScoped<IWalletProcessor, WalletProcessor>();

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
    var dbContext = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
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

Log.Information("WalletService started");
app.Run();