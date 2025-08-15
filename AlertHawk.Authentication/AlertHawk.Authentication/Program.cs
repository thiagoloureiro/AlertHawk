using AlertHawk.Application.Config;
using AlertHawk.Authentication.Helpers;
using AlertHawk.Authentication.Infrastructure.Config;
using AutoMapper.EquivalencyExpression;
using EasyMemoryCache.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", true, false)
    .AddEnvironmentVariables()
    .Build();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDomain();
builder.Services.AddInfrastructure();

builder.Services.AddAutoMapper((_, config) => { config.AddCollectionMappers(); },
    AppDomain.CurrentDomain.GetAssemblies());

var issuers = configuration["Jwt:Issuers"] ??
              "issuer";

var audiences = configuration["Jwt:Audiences"] ??
                "aud";

var key = configuration["Jwt:Key"] ?? "fakeKey";
var sentryEnabled = configuration.GetValue<string>("Sentry:Enabled") ?? "false";

var serviceName = configuration.GetSection("SigNoz:serviceName").Value ?? "AlertHawk.Authentication";
var environment = configuration.GetSection("SigNoz:environment").Value ?? "Development";
var otlpEndpoint = configuration.GetSection("SigNoz:otlpEndpoint").Value;

if (!string.IsNullOrEmpty(otlpEndpoint))
{
// Configure OpenTelemetry with tracing and auto-start.
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource =>
            resource.AddService(serviceName: serviceName,
                    serviceVersion: Assembly.GetEntryAssembly()?.GetName().Version?.ToString())
                .AddAttributes(new Dictionary<string, object>
                {
                    { "deployment.environment", environment }
                }))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation(options =>
            {
                // Configure the ASP.NET Core instrumentation options if needed
                options.RecordException = true; // Record exceptions in traces
                options.EnrichWithHttpRequest = (activity, request) =>
                {
                    // Enrich the activity with additional HTTP request information if needed
                    activity.SetTag("http.method", request.Method);
                    activity.SetTag("http.url", request.Path + request.QueryString);
                };

                // Ignore traces for the /api/version endpoint
                options.Filter = (context) => { return !context.Request.Path.StartsWithSegments("/api/version"); };
            })
            .AddHttpClientInstrumentation()
            .AddSqlClientInstrumentation(options =>
            {
                // Configure SQL client instrumentation options if needed
                options.SetDbStatementForText = true; // Set the SQL statement for text queries
            })
            .AddOtlpExporter(otlpOptions =>
            {
                //SigNoz Cloud Endpoint
                otlpOptions.Endpoint = new Uri(otlpEndpoint);

                otlpOptions.Protocol = OtlpExportProtocol.Grpc;
            }))
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddOtlpExporter(otlpOptions =>
            {
                //SigNoz Cloud Endpoint
                otlpOptions.Endpoint = new Uri(otlpEndpoint);

                otlpOptions.Protocol = OtlpExportProtocol.Grpc;
            }));
}

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

builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();
    c.SwaggerDoc("v1",
        new OpenApiInfo
        {
            Title = "AlertHawk Authentication API",
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

builder.Services.AddEasyCache(configuration.GetSection("CacheSettings").Get<CacheSettings>());

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

var app = builder.Build();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();