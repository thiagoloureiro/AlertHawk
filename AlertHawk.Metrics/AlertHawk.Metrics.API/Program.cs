using System.Reflection;
using AlertHawk.Metrics;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

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
if (string.IsNullOrWhiteSpace(clusterName))
{
    Console.Error.WriteLine("ERROR: CLUSTER_NAME environment variable is required but not set!");
    Console.Error.WriteLine("Please set the CLUSTER_NAME environment variable before starting the application.");
    Environment.Exit(1);
}

builder.Services.AddSingleton<ClickHouseService>(sp => 
    new ClickHouseService(clickHouseConnectionString, clusterName, clickHouseTableName));

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

app.UseHttpsRedirection();
app.MapControllers();

app.Run();