# Backend local development

Prerequisites: .NET 10 SDK, Docker Desktop with the PostgreSQL engine running, and the repository checkout.

```powershell
docker compose up -d postgres
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src/backend/CanadaDeals.Api --urls http://localhost:5099
```

Development settings apply the migration and seed synthetic data. The API health endpoint is `http://localhost:5099/health`.

The worker can be started separately:

```powershell
dotnet run --project src/backend/CanadaDeals.Worker
```

No live retailer credentials or production affiliate destinations belong in local configuration.
