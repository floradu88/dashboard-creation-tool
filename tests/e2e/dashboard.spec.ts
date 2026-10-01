import { expect, test } from '@playwright/test';

test('switches customers, opens a tab, and renders the revenue chart', async ({ page }) => {
  await page.goto('/dashboard/overview?customer=customer-001&from=2026-09-01&to=2026-12-01');
  await expect(page.getByRole('heading', { name: 'Overview' })).toBeVisible();
  await expect(page.getByText('Recognized revenue')).toBeVisible();
  await expect(page.getByRole('img', { name: /Monthly recognized revenue/ })).toBeVisible();
  await expect(page.locator('.chart canvas')).toBeVisible();
  await page.getByRole('link', { name: 'Commercial' }).click();
  await expect(page.getByRole('heading', { name: 'Commercial' })).toBeVisible();
  await page.locator('select[name="customer"]').selectOption('customer-002');
  await page.getByRole('button', { name: 'Apply filters' }).click();
  await expect(page).toHaveURL(/customer=customer-002/);
  await expect(page.getByRole('heading', { name: 'Commercial' })).toBeVisible();
});

test('shows an error for an inverted reporting period', async ({ page }) => {
  await page.goto('/dashboard/overview?customer=customer-001&from=2026-10-01&to=2026-09-01');
  await expect(page.getByRole('alert')).toContainText('could not load');
});

test('keeps section navigation available on a narrow viewport', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/dashboard/news?customer=customer-001&from=2026-09-01&to=2026-10-01');
  await expect(page.getByRole('link', { name: 'Customer news' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Customer news' })).toBeVisible();
});
