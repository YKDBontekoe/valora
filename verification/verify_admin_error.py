from playwright.sync_api import Page, expect, sync_playwright

def test_admin_error_handling(page: Page):
    # 1. Arrange: Go to the Login page.
    page.goto("http://localhost:5173/login")

    # 2. Act: Fill in credentials and click Login.
    page.get_by_label("Email").fill("admin@valora.app")
    page.get_by_label("Password").fill("password")

    # Wait for the button to be clickable
    login_button = page.get_by_role("button", name="Sign in")
    login_button.click()

    # 3. Assert: Wait for the error notification toast.
    # The API call will fail (backend is running but DB is down, so likely 500 or timeout).
    # The NotificationToast should display an error.
    # We look for the toast container or text.
    # Since specific text depends on the error (500: "Server error...", Network: "Network error..."),
    # we'll look for any error toast.

    # Wait for the toast to appear
    # The toast has class 'text-red-800' for error type.
    error_toast = page.locator(".text-red-800")
    expect(error_toast).to_be_visible(timeout=10000)

    # 4. Screenshot: Capture the result.
    page.screenshot(path="verification/verification.png")

if __name__ == "__main__":
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()
        try:
            test_admin_error_handling(page)
        finally:
            browser.close()
