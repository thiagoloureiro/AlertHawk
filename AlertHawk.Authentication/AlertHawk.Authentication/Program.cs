using AlertHawk.Application.Config;
using AlertHawk.Authentication.Helpers;
using AlertHawk.Authentication.Infrastructure.Config;
using AutoMapper.EquivalencyExpression;
using EasyMemoryCache.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDomain();
builder.Services.AddInfrastructure();

builder.Services.AddAutoMapper((_, config) =>
{
    config.AddCollectionMappers();
}, AppDomain.CurrentDomain.GetAssemblies());

var issuer = configuration["Jwt:Issuer"] ?? throw new ArgumentException("Configuration value for 'Jwt:Issuer' not found.");
var audience = configuration["Jwt:Audience"] ?? throw new ArgumentException("Configuration value for 'Jwt:Audience' not found.");
var key = configuration["Jwt:Key"] ?? throw new ArgumentException("Configuration value for 'Jwt:Key' not found.");

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
            ClockSkew = TimeSpan.Zero,
        };
        options.UseSecurityTokenValidators = true;
        options.MapInboundClaims = false;
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

builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AlertHawk Authentication API", Version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
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
