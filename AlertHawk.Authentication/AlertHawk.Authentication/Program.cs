using AlertHawk.Application.Config;
using AlertHawk.Authentication.Infrastructure.Config;
using AutoMapper.EquivalencyExpression;
using EasyMemoryCache.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using AlertHawk.Authentication.Helpers;
using Microsoft.AspNetCore.Authorization;

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

var issuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Configuration value for 'Jwt:Issuer' not found.");
var audience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Configuration value for 'Jwt:Audience' not found.");
var key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Configuration value for 'Jwt:Key' not found.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            RequireExpirationTime = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorizationBuilder()
    .SetDefaultPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireClaim("id")
        .RequireClaim("email")
        .RequireClaim("username")
        .RequireClaim("isAdmin")
        .Build())
    .AddPolicy("AdminPolicy", policy =>
    {
        policy.RequireClaim("id");
        policy.RequireClaim("email");
        policy.RequireClaim("username");
        policy.RequireClaim("isAdmin", "true");
        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
    });

builder.Services.AddEasyCache(configuration.GetSection("CacheSettings").Get<CacheSettings>());
builder.WebHost.UseSentry();

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
