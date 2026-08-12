using CanadaDeals.Infrastructure.Alerts;
using CanadaDeals.Infrastructure.Email;
using CanadaDeals.Infrastructure.Persistence;
using CanadaDeals.Worker;
using Hangfire;
using Hangfire.PostgreSql;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Database") ?? DatabaseServices.DefaultConnection;

builder.Services.AddCanadaDealsPersistence(builder.Configuration);
builder.Services.AddHangfire(configuration => configuration.UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));
builder.Services.AddHangfireServer(options => options.WorkerCount = 1);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddCanadaDealsTransactionalEmail(builder.Configuration, builder.Environment);
builder.Services.AddScoped<PriceAlertEvaluationJob>();
builder.Services.AddSingleton<FixtureJob>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddHealthChecks();

var app = builder.Build();
app.MapHealthChecks("/health");
app.Run();
