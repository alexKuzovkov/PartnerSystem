using MassTransit;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Serilog;
using UserService.Application;
using UserService.Infrastructure;
using UserService.Services;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "database");

builder.Services.AddGrpc();
builder.Services.AddScoped<IUserService, UserService.Application.UserService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });

    options.ListenAnyIP(8081, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1;
    });
});

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
    var dbContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    dbContext.Database.Migrate();
}

app.MapGrpcService<PartnerChainService>();
app.MapHealthChecks("/health");
app.MapControllers();             

var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() => Log.Information("Graceful shutdown..."));
lifetime.ApplicationStopped.Register(() =>
{
    Log.Information("Stopped");
    Log.CloseAndFlush();
});

Log.Information("UserService started (gRPC on :8080, REST on :8081)");
app.Run();