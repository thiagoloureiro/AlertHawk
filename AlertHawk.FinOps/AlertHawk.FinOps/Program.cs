using System.Diagnostics.CodeAnalysis;
using AlertHawk.FinOps;
using FinOpsToolSample.Configuration;
using FinOpsToolSample.Data;
using FinOpsToolSample.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace FinOpsToolSample
{
    [ExcludeFromCodeCoverage]
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers(options =>
            {
                options.Conventions.Insert(0, new GlobalRoutePrefixConvention("finops"));
            });

            // Add services to the container
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new()
                {
                    Title = "AlertHawk FinOps API",
                    Version = "v1",
                    Description = "Azure FinOps Analysis and Cost Optimization API"
                });
            });

            var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", true, false)
            .AddEnvironmentVariables()
            .Build();

            // Configure Azure settings
            builder.Services.Configure<AzureConfiguration>(
                builder.Configuration.GetSection("Azure"));
            builder.Services.Configure<AIConfiguration>(
                builder.Configuration.GetSection("AI"));
            builder.Services.Configure<WeeklyAnalysisOptions>(
                builder.Configuration.GetSection(WeeklyAnalysisOptions.SectionName));

            // Add DbContext
            builder.Services.AddDbContext<FinOpsDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("SqlConnectionString")));

            // Register services
            builder.Services.AddScoped<DatabaseService>();
            builder.Services.AddScoped<IAnalysisOrchestrationService, AnalysisOrchestrationService>();
            builder.Services.AddSingleton<IAnalysisJobService, AnalysisJobService>();
            builder.Services.AddScoped<IDataCleanupService, DataCleanupService>();
            builder.Services.AddHostedService<WeeklySubscriptionAnalysisHostedService>();

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

            // Add CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                               .AllowAnyMethod()
                               .AllowAnyHeader();
                    });
            });

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

            var app = builder.Build();

            // Apply EF migrations or create database on startup
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<Program>>();

                try
                {
                    var context = services.GetRequiredService<FinOpsDbContext>();

                    logger.LogInformation("Checking database status...");

                    var pendingMigrations = context.Database.GetPendingMigrations().ToList();

                    if (pendingMigrations.Any())
                    {
                        logger.LogInformation("Found {Count} pending migrations. Applying...", pendingMigrations.Count);
                        context.Database.Migrate();
                        logger.LogInformation("✅ Database migrations applied successfully");
                    }
                    else
                    {
                        // No migrations exist, use EnsureCreated to create schema from DbContext
                        logger.LogInformation("No migrations found. Ensuring database and tables are created...");
                        var created = context.Database.EnsureCreated();

                        if (created)
                        {
                            logger.LogInformation("✅ Database and tables created successfully");
                        }
                        else
                        {
                            logger.LogInformation("✅ Database already exists and is up to date");
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "❌ An error occurred while initializing the database");
                    throw; // Re-throw to prevent app from starting with database issues
                }
            }

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("AllowAll");
            app.UseHttpsRedirection();
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}