import { test, expect } from '@playwright/test';

test('admin login page renders', async ({ page }) => {
  await page.goto('http://localhost:5173/login');
  await expect(page.locator('h2')).toContainText('Valora Admin');
  await expect(page.locator('button')).toContainText('Login');
});
