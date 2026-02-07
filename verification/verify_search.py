import time
from playwright.sync_api import sync_playwright

def verify_search_screen():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()

        # Navigate to the app
        page.goto("http://localhost:8080")

        # Wait for Flutter to load (simple timeout or specific element if known)
        # Flutter web usually puts a 'flt-glass-pane' in the DOM
        try:
            page.wait_for_selector("flt-glass-pane", state="attached", timeout=30000)
            print("Flutter app loaded.")
        except:
            print("Timed out waiting for flt-glass-pane. Taking screenshot anyway.")

        # Give it a bit more time to render initial frame
        time.sleep(5)

        # Take initial screenshot
        page.screenshot(path="/home/jules/verification/home_screen.png")
        print("Home screen screenshot taken.")

        # Try to click the "Search" tab.
        # In accessibility mode, it might expose "Search" text or aria-label.
        # Flutter web accessibility tree is usually present.

        # Try to find "Search" text.
        # Note: If it's just an icon in the nav bar, it might be tricky.
        # But 'HomeBottomNavBar' likely has labels.

        # Let's try to click by text "Search".
        try:
            # Depending on how the nav bar is built, it might have semantic label.
            # search_tab = page.get_by_text("Search", exact=True)
            # search_tab.click()

            # Or by role button with name Search
            search_button = page.get_by_role("button", name="Search")
            if search_button.count() > 0:
                search_button.first.click()
                print("Clicked Search button.")
            else:
                # Fallback: Click mostly likely location (bottom center-left?)
                # 4 tabs: Home, Search, Saved, Settings.
                # Search is index 1.
                # Screen width / 4 * 1.5 ?
                viewport = page.viewport_size
                if viewport:
                    width = viewport['width']
                    height = viewport['height']
                    # Click at 37.5% width, 95% height
                    page.mouse.click(width * 0.375, height * 0.95)
                    print("Clicked via coordinates.")
                else:
                    print("No viewport size.")

        except Exception as e:
            print(f"Error clicking search: {e}")

        time.sleep(2)
        page.screenshot(path="/home/jules/verification/search_screen_initial.png")

        # Now look for the new Sort button.
        # It's an IconButton with 'sort_rounded' icon. Semantic label is "Sort".
        try:
            sort_btn = page.get_by_role("button", name="Sort")
            # Or by tooltip "Sort"
            if sort_btn.count() > 0:
                sort_btn.first.click()
                print("Clicked Sort button.")
                time.sleep(2) # Wait for bottom sheet
                page.screenshot(path="/home/jules/verification/search_sort_sheet.png")
            else:
                print("Sort button not found by role/name.")

                # If accessibility is not enabled/working well, we might miss it.
                # But typically Flutter web exposes Semantics.

                # Let's try to click top right area where actions are.
                # Actions are usually at right side of AppBar.
                viewport = page.viewport_size
                if viewport:
                    width = viewport['width']
                    # AppBar height is ~56.
                    # Sort button is next to Filter button.
                    # Click top right.
                    page.mouse.click(width - 80, 28)
                    print("Clicked top right via coordinates (Sort?).")
                    time.sleep(2)
                    page.screenshot(path="/home/jules/verification/search_sort_sheet_coord.png")

        except Exception as e:
             print(f"Error clicking sort: {e}")

        browser.close()

if __name__ == "__main__":
    verify_search_screen()
