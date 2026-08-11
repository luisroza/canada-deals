using Npgsql;
using Xunit;

namespace CanadaDeals.Api.IntegrationTests;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequiresPostgresFactAttribute : FactAttribute
{
    private static readonly bool IsAvailable = CheckAvailability();

    public RequiresPostgresFactAttribute()
    {
        if (!IsAvailable) Skip = "PostgreSQL is not available. Start docker compose before running API integration tests.";
    }

    private static bool CheckAvailability()
    {
        var raw = Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION") ?? "Host=localhost;Port=5432;Database=canadadeals;Username=canadadeals;Password=canadadeals";
        var builder = new NpgsqlConnectionStringBuilder(raw) { Timeout = 1, CommandTimeout = 1 };
        try { using var connection = new NpgsqlConnection(builder.ConnectionString); connection.Open(); return true; }
        catch { return false; }
    }
}
