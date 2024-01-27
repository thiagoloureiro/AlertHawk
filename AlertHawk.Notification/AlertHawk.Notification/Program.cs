using AlertHawk.Notification.Domain.Classes;
using AlertHawk.Notification.Domain.Interfaces.Repositories;
using AlertHawk.Notification.Domain.Interfaces.Services;
using AlertHawk.Notification.Infrastructure.Repositories.Class;
using EasyMemoryCache.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using AlertHawk.Notification.Domain.Interfaces.Notifiers;
using AlertHawk.Notification.Infrastructure.Notifiers;
using MassTransit;
using Microsoft.OpenApi.Models;
using SharedModels;

[assembly: ExcludeFromCodeCoverage]
var builder = WebApplication.CreateBuilder(args);

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", true, false)
    .AddEnvironmentVariables()
    .Build();

var rabbitMqHost = configuration.GetValue<string>("RabbitMq:Host");
var rabbitMqUser = configuration.GetValue<string>("RabbitMq:User");
var rabbitMqPass = configuration.GetValue<string>("RabbitMq:Pass");

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<NotificationConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(new Uri($"rabbitmq://{rabbitMqHost}"), h =>
        {
            h.Username(rabbitMqUser);
            h.Password(rabbitMqPass);
        });

        cfg.ReceiveEndpoint("notifications", e => { e.ConfigureConsumer<NotificationConsumer>(context); });
    });
});

// DI
builder.Services.AddEasyCache(configuration.GetSection("CacheSettings").Get<CacheSettings>());

builder.Services.AddTransient<INotificationTypeService, NotificationTypeService>();
builder.Services.AddTransient<INotificationTypeRepository, NotificationTypeRepository>();
builder.Services.AddTransient<INotificationService, NotificationService>();
builder.Services.AddTransient<INotificationRepository, NotificationRepository>();

builder.Services.AddTransient<IMailNotifier, MailNotifier>();
builder.Services.AddTransient<ISlackNotifier, SlackNotifier>();
builder.Services.AddTransient<ITeamsNotifier, TeamsNotifier>();
builder.Services.AddTransient<ITelegramNotifier, TelegramNotifier>();

builder.WebHost.UseSentry();

var app = builder.Build();

app.UseHttpsRedirection();

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

app.UseAuthorization();

app.UseSentryTracing();

app.MapControllers();

app.Run();