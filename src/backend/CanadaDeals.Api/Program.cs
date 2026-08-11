using CanadaDeals.Api.Health;
using CanadaDeals.Api.Services;
using CanadaDeals.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCanadaDealsPersistence(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<CatalogQueryService>();
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("postgresql");

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    await app.Services.ApplyMigrationsAndSeedAsync(app.Configuration.GetValue<bool>("Database:SeedDemoData"));
}

app.UseExceptionHandler("/error");
app.MapGet("/error", () => Results.Problem("An unexpected error occurred."));
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
