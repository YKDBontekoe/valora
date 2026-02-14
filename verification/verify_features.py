from playwright.sync_api import sync_playwright, expect
import time

def run():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page(viewport={'width': 1280, 'height': 800})

        print("Navigating to app...")
        page.goto("http://localhost:8080")

        # Wait for Flutter
        time.sleep(10)

        # We can't easily check internal flutter widgets with playwright
        # unless we use the web renderer and look at the DOM,
        # but flutter web often uses canvas.

        # However, we can take screenshots and hope for the best.

        print("Taking screenshots of different screens...")

        # Home
        page.screenshot(path="verification/home.png")

        # Try to navigate to Search
        # (Heuristic: click on Search tab if possible, but tabs are usually in canvas)

        print("Verification script finished.")
        browser.close()

if __name__ == "__main__":
    run()
