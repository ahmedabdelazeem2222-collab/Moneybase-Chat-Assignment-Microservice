using Microsoft.EntityFrameworkCore;
using MoneyBase.Support.Infrastructure.Extensions;
using Serilog;
using MoneyBase.Support.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

#region Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/app-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();
#endregion

#region IOC container - register services
builder.Services.AddMoneyBaseServices(builder.Configuration)
                .AddHostedServices();
#endregion


builder.Services.AddHttpClient<IGenericHttpClient, GenericHttpClient>(client =>
{
    client.BaseAddress = new Uri("https://localhost:5000/internal"); // replace with actual URL
});

var app = builder.Build();

app.MapGet("/", () => "Hello from MoneyBase.Support.ChatAssignment.APIs");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

try
{
    Log.Information("Starting MoneyBase.Support.ChatAssignment.APIs...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "MoneyBase.Support.ChatAssignment.APIs startup failed!");
}
finally
{
    Log.CloseAndFlush();
}
