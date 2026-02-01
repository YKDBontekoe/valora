from playwright.sync_api import sync_playwright, expect
import time

def verify_auth_screens():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()

        # Navigate to home (Login Screen)
        print("Navigating to http://localhost:8080...")
        page.goto("http://localhost:8080")

        # Wait for Flutter to load (it can be slow)
        print("Waiting for Flutter content...")
        # We can wait for a known element, e.g. "Welcome Back" text
        try:
            # Flutter web uses a canvas, but accessibility tree should be available.
            # Sometimes explicit waits are needed if accessibility is slow to hydrate.
            expect(page.get_by_text("Welcome Back")).to_be_visible(timeout=60000)
            print("Login screen loaded.")
        except Exception as e:
            print(f"Failed to find Welcome Back: {e}")
            page.screenshot(path="verification/error_load.png")
            raise e

        # Screenshot Login Screen
        print("Taking screenshot of Login Screen...")
        time.sleep(2) # Give animations a moment
        page.screenshot(path="verification/login_screen.png")

        # Click "Create Account"
        print("Clicking Create Account...")
        create_account_btn = page.get_by_text("Create Account")
        create_account_btn.click()

        # Wait for "Create Account" header
        try:
            # Using text locator as heading role might not be perfect in Flutter semantics
            expect(page.get_by_text("Join Valora to find your dream home")).to_be_visible(timeout=10000)
            print("Register screen loaded.")
        except Exception as e:
            print(f"Failed to find Register screen content: {e}")
            page.screenshot(path="verification/error_register.png")
            raise e

        # Screenshot Register Screen
        print("Taking screenshot of Register Screen...")
        time.sleep(2)
        page.screenshot(path="verification/register_screen.png")

        browser.close()

if __name__ == "__main__":
    verify_auth_screens()
