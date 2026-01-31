import { test, expect } from '@playwright/test';

test.describe('Visual Regression Tests', () => {
  test('Home page should display listings correctly', async ({ page }) => {
    // Navigate to home
    await page.goto('/');

    // Wait for the specific listing from our seed data to appear
    // This ensures the API call has finished and data is rendered
    await expect(page.getByText('Teststraat 1')).toBeVisible({ timeout: 15000 });

    // Additional wait to ensure images are likely loaded
    await page.waitForTimeout(2000);

    // Take a full page screenshot comparison
    await expect(page).toHaveScreenshot('home-page.png', {
        fullPage: true,
        maxDiffPixelRatio: 0.02
    });
  });

  test('Listing details page should match snapshot', async ({ page }) => {
    await page.goto('/');

    // Wait for list
    await expect(page.getByText('Teststraat 1')).toBeVisible();

    // Click the first listing
    await page.getByText('Teststraat 1').tap();

    // Wait for details content
    await expect(page.getByText('500000')).toBeVisible();
    await expect(page.getByText('3 Bedrooms')).toBeVisible();

    await page.waitForTimeout(1000); // Wait for transition animation

    await expect(page).toHaveScreenshot('listing-details.png', {
        fullPage: true,
        maxDiffPixelRatio: 0.02
    });
  });
});
