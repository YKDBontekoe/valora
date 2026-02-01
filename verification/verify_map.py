from playwright.sync_api import sync_playwright
import time

def run(playwright):
    browser = playwright.chromium.launch(headless=True)
    page = browser.new_page(viewport={'width': 412, 'height': 915}) # Mobile viewport

    print("Navigating to app...")
    page.goto("http://localhost:8081")

    # Wait for Flutter to load (it can be slow)
    print("Waiting for app to load...")
    time.sleep(10) # Gross but effective for Flutter Web CanvasKit

    page.screenshot(path="verification/home_screen.png")
    print("Home screen captured.")

    # Try to click "Search" in bottom nav.
    # Since we can't easily select by text in CanvasKit without semantics,
    # we might have to click by coordinates or guess.
    # But wait! Flutter Web DOES expose accessibility tree to browser.
    # Let's try to find text "Search" or "Valora".

    try:
        # Click Search (Bottom Nav)
        # Position is roughly bottom of screen, 2nd item from left (out of 4 items)
        # Width 412. Items at ~ 50, 150, 250, 350?
        # Let's try clicking by text first.
        search_btn = page.get_by_text("Search", exact=True)
        if search_btn.count() > 0:
            print("Found Search button by text!")
            search_btn.click()
        else:
            print("Text not found, trying click by coordinate (Search icon area)...")
            # Bottom nav is at bottom. Height 915.
            # Click at x=130, y=880 (approx)
            page.mouse.click(130, 880)

    except Exception as e:
        print(f"Error clicking search: {e}")

    print("Waiting for Map screen...")
    time.sleep(5)

    page.screenshot(path="verification/map_screen.png")
    print("Map screen captured.")

    browser.close()

with sync_playwright() as playwright:
    run(playwright)
