using System.Diagnostics.CodeAnalysis;
using AlertHawk.Application.Config;
using AlertHawk.Authentication.Helpers;
using AlertHawk.Authentication.Infrastructure.Config;
using AutoMapper.EquivalencyExpression;
using EasyMemoryCache.Configuration;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Microsoft.Identity.Web;

[assembly: ExcludeFromCodeCoverage]

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

builder.Services.AddAutoMapper((_, config) =>
{
    config.AddCollectionMappers();
}, AppDomain.CurrentDomain.GetAssemblies());

//var issuer = configuration["Jwt:Issuer"] ?? throw new ArgumentException("Configuration value for 'Jwt:Issuer' not found.");
//var issuers = configuration["Jwt:Issuers"] ?? throw new ArgumentException("Configuration value for 'Jwt:Issuers' not found.");
//var audience = configuration["Jwt:Audience"] ?? throw new ArgumentException("Configuration value for 'Jwt:Audience' not found.");
//var audiences = configuration["Jwt:Audiences"] ?? throw new ArgumentException("Configuration value for 'Jwt:Audiences' not found.");
//var key = configuration["Jwt:Key"] ?? throw new ArgumentException("Configuration value for 'Jwt:Key' not found.");

//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddJwtBearer(options =>
//    {
//        options.TokenValidationParameters = new TokenValidationParameters
//        {
//            ValidateIssuer = true,
//            ValidIssuers = issuers.Split(","),
//            //ValidIssuer = issuer,
//            ValidateAudience = true,
//            ValidAudiences = audiences.Split(","),
//            //ValidAudience = audience,
//            ValidateIssuerSigningKey = true,
//            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
//            RequireExpirationTime = true,
//            ValidateLifetime = true,
//            ClockSkew = TimeSpan.Zero,
//        };
//        options.UseSecurityTokenValidators = true;
//        options.MapInboundClaims = false;
//        options.Audience = audience;
//    });

builder.Services.AddMicrosoftIdentityWebApiAuthentication(configuration, jwtBearerScheme: "AzureAd");


builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AlertHawk Authentication API", Version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() });
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
            swaggerDoc.Servers = new List<OpenApiServer> { new OpenApiServer { Url = $"https://{httpReq.Host.Value}{basePath}" } };
        });
    });
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseSentryTracing();

app.MapControllers();

app.Run();
