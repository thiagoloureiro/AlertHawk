using AlertHawk.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register ClickHouse service
var clickHouseConnectionString = Environment.GetEnvironmentVariable("CLICKHOUSE_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("ClickHouse")
    ?? "Host=localhost;Port=8123;Database=default;Username=default;Password=";

var clickHouseTableName = Environment.GetEnvironmentVariable("CLICKHOUSE_TABLE_NAME")
    ?? "k8s_metrics";

builder.Services.AddSingleton<ClickHouseService>(sp => 
    new ClickHouseService(clickHouseConnectionString, clickHouseTableName));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();