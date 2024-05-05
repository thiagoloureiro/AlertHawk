using AlertHawk.Monitoring.Domain.Classes;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Domain.Interfaces.Producers;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using AlertHawk.Monitoring.Helpers;
using AlertHawk.Monitoring.Infrastructure;
using AlertHawk.Monitoring.Infrastructure.MonitorManager;
using AlertHawk.Monitoring.Infrastructure.MonitorRunner;
using AlertHawk.Monitoring.Infrastructure.Producers;
using AlertHawk.Monitoring.Infrastructure.Repositories.Class;
using EasyMemoryCache.Configuration;
using Hangfire;
using MassTransit;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using System.Diagnostics.CodeAnalysis;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;
using AlertHawk.Monitoring.Infrastructure.Utils;
using CustomLog.Client;
using Hangfire.InMemory;

[assembly: ExcludeFromCodeCoverage]
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseSentry(options =>
    {
        options.SetBeforeSend(
            (sentryEvent, _) =>
            {
                if (
                    sentryEvent.Level == SentryLevel.Error
                    && sentryEvent.Logger?.Equals("Microsoft.IdentityModel.LoggingExtensions.IdentityLoggerAdapter",
                        StringComparison.Ordinal) == true
                    && sentryEvent.Message?.Message?.Contains("IDX10223", StringComparison.Ordinal) == true
                )
                {
                    // Do not log 'IDX10223: Lifetime validation failed. The token is expired.'
                    return null;
                }

                return sentryEvent;
            }
        );
    }
);

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", true, false)
    .AddEnvironmentVariables()
    .Build();

var rabbitMqHost = configuration.GetValue<string>("RabbitMq:Host");
var rabbitMqUser = configuration.GetValue<string>("RabbitMq:User");
var rabbitMqPass = configuration.GetValue<string>("RabbitMq:Pass");

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(new Uri($"rabbitmq://{rabbitMqHost}"), h =>
        {
            h.Username(rabbitMqUser);
            h.Password(rabbitMqPass);
        });
    });
});

var azureEnabled = Environment.GetEnvironmentVariable("AZURE_AD_AUTH_ENABLED", EnvironmentVariableTarget.Process) ??
                   "true";
if (azureEnabled == "true")
{
    builder.Services.AddMicrosoftIdentityWebApiAuthentication(configuration, jwtBearerScheme: "AzureAd");
}

builder.Services.AddHangfire(config => config.UseInMemoryStorage(new InMemoryStorageOptions
{
    MaxExpirationTime = TimeSpan.FromMinutes(20)
}));
builder.Services.AddHangfireServer();

builder.Services.AddEasyCache(configuration.GetSection("CacheSettings").Get<CacheSettings>());

builder.Services.AddTransient<IMonitorTypeService, MonitorTypeService>();
builder.Services.AddTransient<IMonitorService, MonitorService>();
builder.Services.AddTransient<IMonitorGroupService, MonitorGroupService>();
builder.Services.AddTransient<IMonitorAgentService, MonitorAgentService>();
builder.Services.AddTransient<IMonitorAlertService, MonitorAlertService>();
builder.Services.AddTransient<IHealthCheckService, HealthCheckService>();
builder.Services.AddTransient<IMonitorReportService, MonitorReportService>();

builder.Services.AddTransient<IMonitorTypeRepository, MonitorTypeRepository>();
builder.Services.AddTransient<IMonitorRepository, MonitorRepository>();
builder.Services.AddTransient<IMonitorAgentRepository, MonitorAgentRepository>();
builder.Services.AddTransient<IMonitorManager, MonitorManager>();
builder.Services.AddTransient<IMonitorGroupRepository, MonitorGroupRepository>();
builder.Services.AddTransient<IMonitorAlertRepository, MonitorAlertRepository>();
builder.Services.AddTransient<IHealthCheckRepository, HealthCheckRepository>();
builder.Services.AddTransient<IMonitorReportRepository, MonitorReportRepository>();

builder.Services.AddTransient<IHttpClientRunner, HttpClientRunner>();
builder.Services.AddTransient<ITcpClientRunner, TcpClientRunner>();
builder.Services.AddScoped<IHttpClientScreenshot, HttpClientScreenshot>();

builder.Services.AddTransient<INotificationProducer, NotificationProducer>();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true; // Enable compression for HTTPS connections
    options.Providers.Add<GzipCompressionProvider>(); // Use Gzip compression
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/json" }); // Compress JSON responses
});

// Add HttpClientFactory
builder.Services.AddHttpClient();
builder.Services.AddCustomLogClient(builder.Configuration.GetSection("CustomLogger"));
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.DefaultIgnoreCondition =
        System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();
    c.SwaggerDoc("v1",
        new OpenApiInfo
        {
            Title = "AlertHawk Monitoring API",
            Version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
        });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description =
            "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 1safsfsdfdfd\"",
    });
    c.OperationFilter<SecurityRequirementsOperationFilter>();
});

GlobalVariables.RandomString = StringUtils.RandomStringGenerator();

var app = builder.Build();

var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();

recurringJobManager.AddOrUpdate<IMonitorManager>("StartMonitorHeartBeatManager", x => x.StartMonitorHeartBeatManager(),
    "*/6 * * * * *");

recurringJobManager.AddOrUpdate<IMonitorManager>("StartMasterMonitorAgentTaskManager",
    x => x.StartMasterMonitorAgentTaskManager(), "*/10 * * * * *");

recurringJobManager.AddOrUpdate<IMonitorManager>("StartRunnerManager", x => x.StartRunnerManager(), "*/25 * * * * *");

recurringJobManager.AddOrUpdate<IMonitorService>("SetMonitorDashboardDataCacheList",
    x => x.SetMonitorDashboardDataCacheList(), "*/5 * * * *");

// Resolve the service and run the method immediately
using (var serviceScope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
{
    var monitorService = serviceScope.ServiceProvider.GetService<IMonitorService>();
    monitorService?.SetMonitorDashboardDataCacheList();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMiddleware<SwaggerBasicAuthMiddleware>();
    var basePath = Environment.GetEnvironmentVariable("basePath") ?? "";
    app.UseSwagger(c =>
    {
        c.RouteTemplate = "swagger/{documentName}/swagger.json";
        c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
        {
            swaggerDoc.Servers = new List<OpenApiServer>
                { new OpenApiServer { Url = $"https://{httpReq.Host.Value}{basePath}" } };
        });
    });
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.UseResponseCompression();

app.Run();