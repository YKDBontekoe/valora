from playwright.sync_api import sync_playwright
import time

def run():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page(viewport={'width': 1280, 'height': 800})

        print("Navigating to app...")
        page.goto("http://localhost:8080")

        # Wait for Flutter to load (it renders into a <flt-glass-pane> or just canvas)
        print("Waiting for Flutter to load...")
        # Try to wait for the shadow root or canvas
        try:
            # This is a heuristic. Flutter web usually adds `flt-glass-pane`
            page.wait_for_selector('flt-glass-pane', timeout=10000)
            print("Found flutter glass pane.")
        except:
            print("Did not find glass pane, waiting a bit more...")
            time.sleep(5)

        # Give it a bit more time for animations to settle
        time.sleep(5)

        print("Taking screenshot...")
        page.screenshot(path="verification/verification.png")
        print("Screenshot saved.")

        browser.close()

if __name__ == "__main__":
    run()
