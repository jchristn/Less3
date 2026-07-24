import { expect, Page, test } from '@playwright/test';

const API_ENDPOINT = 'http://127.0.0.1:8000';
const ADMIN_KEY = 'less3admin';

const emptyArray = JSON.stringify([]);
const jsonHeaders = { 'content-type': 'application/json' };

async function mockLess3(page: Page): Promise<void> {
  await page.route('**/admin/requesthistory/summary**', async (route) => {
    await route.fulfill({
      status: 200,
      headers: jsonHeaders,
      body: JSON.stringify({
        TotalSuccess: 0,
        TotalFailure: 0,
        Buckets: [],
        Intervals: [],
        GeneratedUtc: '2026-07-22T00:00:00.000Z',
      }),
    });
  });

  await page.route('**/admin/stats', async (route) => {
    await route.fulfill({
      status: 200,
      headers: jsonHeaders,
      body: JSON.stringify({
        BucketCount: 0,
        TotalObjectCount: 0,
        TotalBytes: 0,
        GeneratedUtc: '2026-07-22T00:00:00.000Z',
        Buckets: [],
      }),
    });
  });

  await page.route('**/admin/health', async (route) => {
    await route.fulfill({
      status: 200,
      headers: jsonHeaders,
      body: JSON.stringify({
        ServerVersion: '3.0.0',
        UptimeSeconds: 30,
        DatabaseType: 'Sqlite',
        DatabaseReachable: true,
        StoragePath: '/tmp/less3',
        StoragePathWritable: true,
        FreeDiskBytes: 1024,
        TempPath: '/tmp/less3/temp',
        TempUploadCount: 0,
        RequestHistoryRetentionDays: 30,
        LastCleanupRunUtc: null,
        GeneratedUtc: '2026-07-22T00:00:00.000Z',
      }),
    });
  });

  await page.route('**/admin/reports/requests**', async (route) => {
    await route.fulfill({
      status: 200,
      headers: jsonHeaders,
      body: JSON.stringify({
        TenantId: 'default',
        RequestCount: 0,
        SuccessCount: 0,
        FailureCount: 0,
        RequestsPerMinute: 0,
        FailureRate: 0,
        P50LatencyMs: 0,
        P95LatencyMs: 0,
        TopBucketsByBytes: [],
        TopBucketsByRequestCount: [],
        TopFailedRequestTypes: [],
        TopAccessKeys: [],
        GeneratedUtc: '2026-07-22T00:00:00.000Z',
      }),
    });
  });

  await page.route('**/admin/maintenance**', async (route) => {
    await route.fulfill({
      status: 200,
      headers: jsonHeaders,
      body: JSON.stringify({
        RequestHistoryRetentionDays: 30,
        CleanupIntervalMs: 3600000,
        LastCleanupRunUtc: null,
        RuntimeEditableSettings: ['RequestHistoryRetentionDays', 'CleanupIntervalMs'],
        RestartRequiredSettings: ['Database', 'Storage.DiskDirectory'],
        Configuration: {},
        GeneratedUtc: '2026-07-22T00:00:00.000Z',
      }),
    });
  });

  await page.route('**/admin/users**', async (route) => {
    await route.fulfill({ status: 200, headers: jsonHeaders, body: emptyArray });
  });

  await page.route('**/admin/credentials**', async (route) => {
    await route.fulfill({ status: 200, headers: jsonHeaders, body: emptyArray });
  });

  await page.route('**/admin/buckets**', async (route) => {
    const requestUrl = route.request().url();
    if (/\/admin\/buckets\/[^/?]+/.test(requestUrl)) {
      await route.fulfill({
        status: 200,
        headers: jsonHeaders,
        body: JSON.stringify({
          Id: 'bkt_test',
          TenantId: 'default',
          Name: 'test-bucket',
          EnableVersioning: true,
          CreatedUtc: '2026-07-22T00:00:00.000Z',
        }),
      });
      return;
    }

    await route.fulfill({
      status: 200,
      headers: jsonHeaders,
      body: JSON.stringify([
        {
          Id: 'bkt_test',
          TenantId: 'default',
          Name: 'test-bucket',
          EnableVersioning: true,
          CreatedUtc: '2026-07-22T00:00:00.000Z',
        },
      ]),
    });
  });

  await page.route('**/admin/requesthistory**', async (route) => {
    await route.fulfill({ status: 200, headers: jsonHeaders, body: emptyArray });
  });

  await page.route('**/admin/tenants**', async (route) => {
    await route.fulfill({
      status: 200,
      headers: jsonHeaders,
      body: JSON.stringify([
        {
          Id: 'default',
          Name: 'Default',
          Active: true,
          CreatedUtc: '2026-07-22T00:00:00.000Z',
        },
      ]),
    });
  });

  await page.route('**/admin/roles**', async (route) => {
    await route.fulfill({ status: 200, headers: jsonHeaders, body: emptyArray });
  });

  await page.route('**/admin/roleassignments**', async (route) => {
    await route.fulfill({ status: 200, headers: jsonHeaders, body: emptyArray });
  });

  await page.route('**/admin/permissions**', async (route) => {
    await route.fulfill({ status: 200, headers: jsonHeaders, body: emptyArray });
  });

  await page.route('**/*', async (route) => {
    const request = route.request();
    if (request.url().startsWith(API_ENDPOINT)) {
      await route.fulfill({ status: 200, headers: jsonHeaders, body: emptyArray });
      return;
    }

    await route.continue();
  });
}

async function seedDashboardSession(page: Page): Promise<void> {
  await page.addInitScript(
    ({ endpoint, adminKey }) => {
      window.localStorage.setItem('less3APIUrl', endpoint);
      window.localStorage.setItem('less3AdminApiKey', adminKey);
    },
    { endpoint: API_ENDPOINT, adminKey: ADMIN_KEY }
  );
}

async function expectNoHorizontalOverflow(page: Page): Promise<void> {
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
  expect(overflow).toBeLessThanOrEqual(2);
}

test.describe('Less3 dashboard smoke', () => {
  for (const viewport of [
    { width: 1280, height: 900 },
    { width: 768, height: 900 },
    { width: 390, height: 900 },
  ]) {
    test(`login is responsive at ${viewport.width}px`, async ({ page }) => {
      await page.setViewportSize(viewport);
      await mockLess3(page);
      await page.goto('/');

      await expect(page.getByRole('heading', { name: 'Admin Sign In' })).toBeVisible();
      await expect(page.getByLabel('Less3 Server URL')).toBeVisible();
      await expect(page.getByLabel('Admin API Key')).toBeVisible();
      await expect(page.getByRole('button', { name: /sign in to dashboard/i })).toBeVisible();
      await expectNoHorizontalOverflow(page);
    });
  }

  test('login authenticates and logout is icon-only but accessible', async ({ page }) => {
    await mockLess3(page);
    await page.goto('/');
    await page.getByLabel('Less3 Server URL').fill(API_ENDPOINT);
    await page.getByLabel('Admin API Key').fill(ADMIN_KEY);
    await page.getByRole('button', { name: /sign in to dashboard/i }).click();

    await expect(page).toHaveURL(/\/dashboard$/);
    await expect(page.getByRole('heading', { name: 'Home' })).toBeVisible();

    const logout = page.getByRole('button', { name: 'Logout' });
    await expect(logout).toBeVisible();
    await expect(logout).not.toContainText(/logout/i);
  });

  for (const route of [
    { path: '/dashboard', heading: 'Home' },
    { path: '/admin/tenants', heading: 'Tenants' },
    { path: '/admin/buckets', heading: 'Buckets' },
    { path: '/admin/buckets/bkt_test?name=test-bucket', heading: 'Bucket: test-bucket' },
    { path: '/admin/objects', heading: 'Objects' },
    { path: '/admin/request-history', heading: 'Request History' },
    { path: '/admin/maintenance', heading: 'Maintenance' },
    { path: '/admin/api-explorer', heading: 'API Explorer' },
    { path: '/admin/users', heading: 'Users' },
    { path: '/admin/credentials', heading: 'Credentials' },
    { path: '/admin/roles', heading: 'Roles' },
    { path: '/admin/role-assignments', heading: 'Role Assignments' },
    { path: '/admin/permissions', heading: 'Permissions' },
  ]) {
    test(`${route.heading} route renders`, async ({ page }) => {
      await mockLess3(page);
      await seedDashboardSession(page);
      await page.goto(route.path);

      await expect(page.getByRole('heading', { name: route.heading })).toBeVisible();
      await expectNoHorizontalOverflow(page);
    });
  }
});
