using CanadaDeals.Infrastructure.Persistence;
using CanadaDeals.Worker;
using Hangfire;
using Hangfire.PostgreSql;

var builder = Host.CreateApplicationBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Database") ?? DatabaseServices.DefaultConnection;

builder.Services.AddCanadaDealsPersistence(builder.Configuration);
builder.Services.AddHangfire(configuration => configuration.UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));
builder.Services.AddHangfireServer(options => options.WorkerCount = 1);
builder.Services.AddSingleton<FixtureJob>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
