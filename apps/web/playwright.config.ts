import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./e2e",
  use: {
    baseURL: "http://localhost:3000",
    trace: "retain-on-failure",
  },
  webServer: [
    {
      command: "dotnet run --project ../../src/backend/CanadaDeals.Api --urls http://localhost:5099",
      url: "http://localhost:5099/health",
      reuseExistingServer: false,
      env: { ASPNETCORE_ENVIRONMENT: "Development", Email__AutoConfirmDevelopmentAccounts: "false", AuthenticationRateLimit__PermitLimit: "1000" },
    },
    {
      command: "dotnet run --project ../../src/backend/CanadaDeals.Worker --urls http://localhost:5100",
      url: "http://localhost:5100/health",
      reuseExistingServer: false,
      env: { ASPNETCORE_ENVIRONMENT: "Development" },
    },
    {
      command: "pnpm dev",
      url: "http://localhost:3000",
      reuseExistingServer: true,
      cwd: ".",
    },
  ],
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
});
