var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.WebHost.UseSentry(options => options.SetBeforeSend(
    (sentryEvent, _) =>
    {
        if (sentryEvent.Message?.Message?.Contains("IDX10223", StringComparison.Ordinal) == true)
        {   // Do not log 'IDX10223: Lifetime validation failed. The token is expired.'
            return null;
        }
        return sentryEvent;
    }
));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.UseSentryTracing();

app.Run();