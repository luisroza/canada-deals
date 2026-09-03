import { defineConfig, devices } from "@playwright/test";

const e2eDatabase = process.env.E2E_DATABASE_CONNECTION
  ?? "Host=localhost;Port=5432;Database=canadadeals_e2e;Username=canadadeals;Password=canadadeals";
const apiPort = process.env.E2E_API_PORT ?? "5099";
const workerPort = process.env.E2E_WORKER_PORT ?? "5100";
const webPort = process.env.E2E_WEB_PORT ?? "3000";
const apiOrigin = `http://localhost:${apiPort}`;
const workerOrigin = `http://localhost:${workerPort}`;
const webOrigin = `http://localhost:${webPort}`;

export default defineConfig({
  testDir: "./e2e",
  use: {
    baseURL: webOrigin,
    trace: "retain-on-failure",
  },
  webServer: [
    {
      command: `dotnet run --project ../../src/backend/CanadaDeals.Api --urls ${apiOrigin}`,
      url: `${apiOrigin}/health`,
      reuseExistingServer: false,
      env: { ASPNETCORE_ENVIRONMENT: "Development", ConnectionStrings__Database: e2eDatabase, Email__AutoConfirmDevelopmentAccounts: "false", AuthenticationRateLimit__PermitLimit: "1000" },
    },
    {
      command: `dotnet run --project ../../src/backend/CanadaDeals.Worker --urls ${workerOrigin}`,
      url: `${workerOrigin}/health`,
      reuseExistingServer: false,
      env: { ASPNETCORE_ENVIRONMENT: "Development", ConnectionStrings__Database: e2eDatabase },
    },
    {
      command: `pnpm exec next dev -p ${webPort}`,
      url: webOrigin,
      reuseExistingServer: true,
      cwd: ".",
      env: { API_ORIGIN: apiOrigin, API_BASE_URL: apiOrigin },
    },
  ],
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
});
