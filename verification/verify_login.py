from playwright.sync_api import Page, expect, sync_playwright
import time

def verify_login_screen(page: Page):
    print("Navigating to http://localhost:3000...")
    page.goto("http://localhost:3000")

    print("Waiting for 'Welcome Back' text...")
    # Increase timeout for Flutter initialization
    try:
        expect(page.get_by_text("Welcome Back")).to_be_visible(timeout=30000)
        print("'Welcome Back' found.")
    except Exception as e:
        print(f"Could not find text: {e}")
        # Take a screenshot anyway to see what's there

    # Wait a bit more for fonts and animations to settle
    time.sleep(2)

    print("Taking screenshot...")
    page.screenshot(path="verification/verification.png")
    print("Screenshot saved to verification/verification.png")

if __name__ == "__main__":
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()
        # Set viewport to a mobile-like size since it's a mobile app design
        page.set_viewport_size({"width": 375, "height": 812})
        try:
            verify_login_screen(page)
        finally:
            browser.close()
