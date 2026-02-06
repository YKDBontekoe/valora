from playwright.sync_api import sync_playwright
import time

def run():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()

        print("Navigating to app...")
        page.goto("http://localhost:8080")

        # Wait for Flutter to load
        print("Waiting for flt-glass-pane...")
        try:
            page.wait_for_selector("flt-glass-pane", timeout=30000)
            print("Flutter loaded.")
        except:
            print("Timeout waiting for Flutter. Taking screenshot anyway.")

        # Give it a bit more time to render initial frame
        time.sleep(5)

        # Take screenshot of Home Screen
        page.screenshot(path="verification/home_screen.png")
        print("Screenshot saved to verification/home_screen.png")

        browser.close()

if __name__ == "__main__":
    run()
