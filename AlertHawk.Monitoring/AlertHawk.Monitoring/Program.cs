using AlertHawk.Monitoring;
using AlertHawk.Monitoring.Domain.Classes;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Domain.Interfaces.Producers;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using AlertHawk.Monitoring.Helpers;
using AlertHawk.Monitoring.Infrastructure.MonitorRunner;
using AlertHawk.Monitoring.Infrastructure.Producers;
using AlertHawk.Monitoring.Infrastructure.Utils;
using EasyMemoryCache.Configuration;
using Hangfire;
using Hangfire.InMemory;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SharedModels;

var builder = WebApplication.CreateBuilder(args);

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", true, false)
    .AddEnvironmentVariables()
    .Build();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddEventSourceLogger();

var rabbitMqHost = configuration.GetValue<string>("RabbitMq:Host");
var rabbitMqUser = configuration.GetValue<string>("RabbitMq:User");
var rabbitMqPass = configuration.GetValue<string>("RabbitMq:Pass");
var sentryEnabled = configuration.GetValue<string>("Sentry:Enabled") ?? "false";
var queueType = configuration.GetValue<string>("QueueType") ?? "RABBITMQ";
var serviceBusConnectionString = configuration.GetValue<string>("ServiceBus:ConnectionString");
var serviceBusQueueName = configuration.GetValue<string>("ServiceBus:QueueName");

var cacheFrequency = configuration.GetValue<string>("DataCacheFrequencyCron") ?? "*/2 * * * *";

if (string.Equals(sentryEnabled, "true", StringComparison.InvariantCultureIgnoreCase))
{
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
                        || sentryEvent.Message?.Message?.Contains("IDX10205", StringComparison.Ordinal) == true
                        || sentryEvent.Message?.Message?.Contains("IDX10503", StringComparison.Ordinal) == true
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
}

Console.WriteLine("Starting MassTransit Configuration");

builder.Services.AddMassTransit(x =>
{
    x.DisableUsageTelemetry();

    switch (queueType.ToUpper())
    {
        case "RABBITMQ":
            x.UsingRabbitMq((_, cfg) =>
            {
                Console.WriteLine($"Connecting to RabbitMQ at {rabbitMqHost}");
                cfg.Host(new Uri($"rabbitmq://{rabbitMqHost}"), h =>
                {
                    if (rabbitMqUser != null) h.Username(rabbitMqUser);
                    if (rabbitMqPass != null) h.Password(rabbitMqPass);
                });
            });
            break;

        case "SERVICEBUS":
            x.UsingAzureServiceBus((context, cfg) =>
            {
                Console.WriteLine($"Connecting to Azure Service Bus");
                cfg.Host(serviceBusConnectionString);
                cfg.Message<NotificationAlert>(config =>
                {
                    config.SetEntityName(serviceBusQueueName);
                });
                cfg.Message<NotificationAlert>(c => c.SetEntityName("notificationsTopic"));
            });
            break;
    }
});

var issuers = configuration["Jwt:Issuers"] ??
              "issuer";

var audiences = configuration["Jwt:Audiences"] ??
                "aud";

var key = configuration["Jwt:Key"] ?? "fakeKey";

// Add services to the container
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer("JwtBearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = issuers.Split(","),
            ValidateAudience = true,
            ValidAudiences = audiences.Split(","),
            ValidateIssuerSigningKey = false,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            RequireExpirationTime = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
    })
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"), jwtBearerScheme: "AzureAd");

builder.Services.AddAuthorization(options =>
{
    var defaultAuthorizationPolicyBuilder = new AuthorizationPolicyBuilder(
        "JwtBearer",
        "AzureAd"
    );
    defaultAuthorizationPolicyBuilder = defaultAuthorizationPolicyBuilder.RequireAuthenticatedUser();
    options.DefaultPolicy = defaultAuthorizationPolicyBuilder.Build();
});

builder.Services.AddHangfire(config => config.UseInMemoryStorage(new InMemoryStorageOptions
{
    MaxExpirationTime = TimeSpan.FromMinutes(20)
}));
builder.Services.AddHangfireServer();

builder.Services.AddEasyCache(configuration.GetSection("CacheSettings").Get<CacheSettings>());

builder.Services.AddCustomServices();
builder.Services.AddCustomRepositories();

builder.Services.AddTransient<IHttpClientRunner, HttpClientRunner>();
builder.Services.AddTransient<ITcpClientRunner, TcpClientRunner>();
builder.Services.AddTransient<IK8sClientRunner, K8sClientRunner>();

builder.Services.AddTransient<INotificationProducer, NotificationProducer>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true; // Enable compression for HTTPS connections
    options.Providers.Add<GzipCompressionProvider>(); // Use Gzip compression
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/json" }); // Compress JSON responses
});

// Add HttpClientFactory
builder.Services.AddHttpClient();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.DefaultIgnoreCondition =
        JsonIgnoreCondition.WhenWritingNull;
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
    x => x.SetMonitorDashboardDataCacheList(), cacheFrequency);

recurringJobManager.AddOrUpdate<IMonitorManager>("CleanMonitorHistoryTask",
    x => x.CleanMonitorHistoryTask(), "0 0 * * *");

// Resolve the service and run the method immediately
using (var serviceScope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
{
    var monitorService = serviceScope.ServiceProvider.GetService<IMonitorService>();
    monitorService?.SetMonitorDashboardDataCacheList();
    
    // Initialize SystemConfiguration table if it doesn't exist
    var systemConfigurationRepository = serviceScope.ServiceProvider.GetService<ISystemConfigurationRepository>();
    if (systemConfigurationRepository != null)
    {
        await systemConfigurationRepository.InitializeTableIfNotExists();
    }
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