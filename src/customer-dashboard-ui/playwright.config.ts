import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: '../../tests/e2e',
  timeout: 60_000,
  expect: { timeout: 15_000 },
  use: { ...devices['Desktop Chrome'], baseURL: 'http://127.0.0.1:4200' },
  webServer: [
    {
      command: 'dotnet run --project ../CustomerDashboard.Api/CustomerDashboard.Api.csproj --urls http://127.0.0.1:5080',
      url: 'http://127.0.0.1:5080/health/ready',
      reuseExistingServer: true,
      timeout: 120_000,
    },
    {
      command: 'npx ng serve --host 127.0.0.1 --port 4200 --proxy-config proxy.conf.json',
      url: 'http://127.0.0.1:4200',
      reuseExistingServer: true,
      timeout: 120_000,
    },
  ],
});
