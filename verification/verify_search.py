from playwright.sync_api import sync_playwright
import time

def verify_search_screen(page):
    print("Navigating to http://localhost:8080")
    page.goto("http://localhost:8080")

    print("Waiting for app to load...")
    time.sleep(15)

    print("Taking screenshot...")
    page.screenshot(path="verification/search_screen.png")

if __name__ == "__main__":
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()
        try:
            verify_search_screen(page)
        finally:
            browser.close()
