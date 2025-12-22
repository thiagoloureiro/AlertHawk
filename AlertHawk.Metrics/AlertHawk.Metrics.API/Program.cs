using AlertHawk.Metrics.API.Producers;
using AlertHawk.Metrics.API.Repositories;
using AlertHawk.Metrics.API.Services;
using EasyMemoryCache.Configuration;
using Hangfire;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SharedModels;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", true, false)
    .AddEnvironmentVariables()
    .Build();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

builder.WebHost.UseSentry(options =>
    {
        options.SetBeforeSend((sentryEvent, _) =>
            {
                if (
                    sentryEvent.Level == SentryLevel.Error
                    && sentryEvent.Logger?.Equals("Microsoft.IdentityModel.LoggingExtensions.IdentityLoggerAdapter",
                        StringComparison.Ordinal) == true
                    && sentryEvent.Message?.Message?.Contains("IDX10223", StringComparison.Ordinal) == true
                    || sentryEvent.Message?.Message?.Contains("IDX10205", StringComparison.Ordinal) == true
                    || sentryEvent.Message?.Message?.Contains("IDX10214", StringComparison.Ordinal) == true
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

builder.Services.AddEasyCache(configuration.GetSection("CacheSettings").Get<CacheSettings>());

// Configure MassTransit
var rabbitMqHost = configuration.GetValue<string>("RabbitMq:Host");
var rabbitMqUser = configuration.GetValue<string>("RabbitMq:User");
var rabbitMqPass = configuration.GetValue<string>("RabbitMq:Pass");
var queueType = configuration.GetValue<string>("QueueType") ?? "RABBITMQ";
var serviceBusConnectionString = configuration.GetValue<string>("ServiceBus:ConnectionString");
var serviceBusQueueName = configuration.GetValue<string>("ServiceBus:QueueName");

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

// Register NodeStatusTracker and NotificationProducer
builder.Services.AddSingleton<NodeStatusTracker>();
builder.Services.AddScoped<IMetricsNotificationRepository, MetricsNotificationRepository>();
builder.Services.AddScoped<IMetricsNotificationService, MetricsNotificationService>();
builder.Services.AddScoped<IMetricsAlertRepository, MetricsAlertRepository>();
builder.Services.AddScoped<IMetricsAlertService, MetricsAlertService>();
builder.Services.AddScoped<INotificationProducer, NotificationProducer>();

builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();
    c.SwaggerDoc("v1",
        new OpenApiInfo
        {
            Title = "AlertHawk Metrics API",
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
});

// Register ClickHouse service
var clickHouseConnectionString = Environment.GetEnvironmentVariable("CLICKHOUSE_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("ClickHouse")
    ?? "Host=localhost;Port=8123;Database=default;Username=default;Password=";

var clickHouseTableName = Environment.GetEnvironmentVariable("CLICKHOUSE_TABLE_NAME")
    ?? "k8s_metrics";

var clusterName = Environment.GetEnvironmentVariable("CLUSTER_NAME");

builder.Services.AddSingleton<IClickHouseService>(sp =>
    new ClickHouseService(clickHouseConnectionString, clusterName, clickHouseTableName));

// Register Azure Prices service
builder.Services.AddHttpClient<IAzurePricesService, AzurePricesService>();

// Configure Hangfire for background jobs
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseInMemoryStorage());

builder.Services.AddHangfireServer();

// Register Log Cleanup Service
var enableLogCleanup = Environment.GetEnvironmentVariable("ENABLE_LOG_CLEANUP");

var cronExpression = Environment.GetEnvironmentVariable("LOG_CLEANUP_INTERVAL_HOURS") ?? "0 0 * * *";

builder.Services.AddSingleton<LogCleanupService>(sp =>
{
    var clickHouseService = sp.GetRequiredService<IClickHouseService>();
    var logger = sp.GetRequiredService<ILogger<LogCleanupService>>();
    return new LogCleanupService(clickHouseService, logger);
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
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

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

// Schedule recurring log cleanup job if enabled
if (bool.TryParse(enableLogCleanup, out var isEnabled) && isEnabled)
{
    var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();
    var cleanupService = app.Services.GetRequiredService<LogCleanupService>();

    recurringJobManager.AddOrUpdate(
        "log-cleanup-job",
        () => cleanupService.CleanupLogsAsync(),
        cronExpression,
        new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.Utc
        });

    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation(
        $"System log cleanup job scheduled. (truncates ClickHouse system log tables), Cron: {cronExpression}",
        cronExpression);
}
else
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("System log cleanup is disabled. Set ENABLE_LOG_CLEANUP=true to enable.");
}

app.MapControllers();

app.Run();