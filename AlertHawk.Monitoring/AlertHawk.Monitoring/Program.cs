using AlertHawk.Monitoring.Domain.Classes;
using AlertHawk.Monitoring.Domain.Interfaces.MonitorRunners;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;
using AlertHawk.Monitoring.Helpers;
using AlertHawk.Monitoring.Infrastructure.MonitorManager;
using AlertHawk.Monitoring.Infrastructure.MonitorRunner;
using AlertHawk.Monitoring.Infrastructure.Repositories.Class;
using EasyMemoryCache.Configuration;
using Hangfire;
using Hangfire.MemoryStorage;
using MassTransit;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseSentry();
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

builder.Services.AddHangfire(config => config.UseMemoryStorage());
builder.Services.AddEasyCache(configuration.GetSection("CacheSettings").Get<CacheSettings>());

builder.Services.AddTransient<IMonitorTypeService, MonitorTypeService>();
builder.Services.AddTransient<IMonitorService, MonitorService>();
builder.Services.AddTransient<IMonitorGroupService, MonitorGroupService>();
builder.Services.AddTransient<IMonitorAgentService, MonitorAgentService>();


builder.Services.AddTransient<IMonitorTypeRepository, MonitorTypeRepository>();
builder.Services.AddTransient<IMonitorRepository, MonitorRepository>();
builder.Services.AddTransient<IMonitorAgentRepository, MonitorAgentRepository>();
builder.Services.AddTransient<IMonitorManager, MonitorManager>();
builder.Services.AddTransient<IMonitorGroupRepository, MonitorGroupRepository>();

builder.Services.AddTransient<IHttpClientRunner, HttpClientRunner>();
builder.Services.AddTransient<ITcpClientRunner, TcpClientRunner>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseHangfireDashboard();
app.UseHangfireServer();

RecurringJob.AddOrUpdate<IMonitorManager>(x => x.StartMonitorHeartBeatManager(), "*/6 * * * * *");
RecurringJob.AddOrUpdate<IMonitorManager>(x => x.StartMasterMonitorAgentTaskManager(), "*/10 * * * * *");
RecurringJob.AddOrUpdate<IMonitorManager>(x => x.StartRunnerManager(), "*/25 * * * * *");

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

app.UseAuthorization();
app.UseSentryTracing();

app.MapControllers();

app.Run();

