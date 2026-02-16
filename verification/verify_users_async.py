from playwright.sync_api import sync_playwright

def run(playwright):
    browser = playwright.chromium.launch(headless=True)
    context = browser.new_context()
    page = context.new_page()

    # Intercept API calls to /api/admin/users
    page.route("**/api/admin/users*", lambda route: route.fulfill(
        status=200,
        content_type="application/json",
        body='{"items": [{"id": "user-1", "email": "user1@example.com", "roles": ["Admin"]}, {"id": "user-2", "email": "user2@example.com", "roles": ["User"]}], "totalPages": 1, "page": 1, "totalCount": 2}'
    ))

    # Store the delete route so we can fulfill it manually
    delete_route_wrapper = []
    def handle_delete(route):
        print(f"Intercepted DELETE request to {route.request.url}")
        delete_route_wrapper.append(route)
        # Do not fulfill yet!

    page.route("**/api/admin/users/user-2", handle_delete)

    # Pre-populate localStorage
    page.add_init_script("""
        localStorage.setItem('admin_token', 'mock-token');
        localStorage.setItem('admin_userId', 'user-1');
    """)

    try:
        # Navigate to the Users page
        print("Navigating to Users page...")
        page.goto("http://localhost:5173/users")

        # Click delete on user-2
        print("Waiting for user row...")
        user2_row = page.locator("tr").filter(has_text="user2@example.com")
        user2_row.wait_for(timeout=10000)

        print("Clicking delete button...")
        delete_btn = user2_row.get_by_role("button")
        delete_btn.click()

        # Wait for dialog
        print("Waiting for dialog...")
        dialog = page.get_by_role("dialog")
        dialog.wait_for()

        # Click Confirm (this triggers the DELETE request)
        print("Clicking Confirm...")
        confirm_btn = dialog.get_by_role("button", name="Delete")
        confirm_btn.click()

        # Now the request should be intercepted and hanging.
        # Verify the button is disabled and shows loading spinner?
        print("Verifying loading state...")
        try:
            # Check if button is disabled
            page.wait_for_function("document.querySelector('div[role=\"dialog\"] button.bg-red-600').disabled === true", timeout=5000)
            print("Confirm button is disabled (loading state).")
            # Verify spinner exists
            # We can check for lucide-loader2 inside the button?
            # Or just take screenshot.
            page.screenshot(path="verification/loading_state.png")
            print("Screenshot saved: verification/loading_state.png")
        except Exception as e:
            print(f"Error checking disabled state: {e}")
            page.screenshot(path="verification/debug_loading_failed.png")

        # Fulfill the request
        if delete_route_wrapper:
            print("Fulfilling DELETE request...")
            delete_route_wrapper[0].fulfill(status=200)
        else:
            print("Did not intercept DELETE request!")

        # Dialog should close
        print("Waiting for dialog to close...")
        dialog.wait_for(state="hidden", timeout=5000)
        print("Dialog closed successfully.")

    except Exception as e:
        print(f"Test failed: {e}")
        page.screenshot(path="verification/failure.png")
    finally:
        browser.close()

if __name__ == "__main__":
    with sync_playwright() as playwright:
        run(playwright)
