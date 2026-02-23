from playwright.sync_api import sync_playwright

def run(playwright):
    browser = playwright.chromium.launch(headless=True)
    context = browser.new_context()

    # Inject auth token to bypass login
    context.add_init_script("""
        localStorage.setItem('admin_token', 'mock-token');
        localStorage.setItem('admin_userId', 'mock-user-id');
    """)

    page = context.new_page()

    # Intercept API calls to mock responses
    page.route("**/api/health", lambda route: route.fulfill(
        status=200,
        content_type="application/json",
        body='{"status": "Healthy", "database": "Healthy", "apiLatency": 10, "apiLatencyP50": 10, "apiLatencyP95": 20, "apiLatencyP99": 30, "activeJobs": 0, "queuedJobs": 0, "failedJobs": 0, "lastPipelineSuccess": "2023-01-01T00:00:00Z", "timestamp": "2023-01-01T00:00:00Z"}'
    ))

    # Mock jobs response
    mock_jobs = {
        "items": [
            {
                "id": "1",
                "type": "CityIngestion",
                "status": "Completed",
                "target": "Amsterdam",
                "progress": 100,
                "createdAt": "2023-01-01T12:00:00Z",
                "resultSummary": "Processed 50 neighborhoods"
            },
            {
                "id": "2",
                "type": "MapGeneration",
                "status": "Processing",
                "target": "Rotterdam",
                "progress": 45,
                "createdAt": "2023-01-02T10:00:00Z",
                "resultSummary": None
            }
        ],
        "pageIndex": 1,
        "totalPages": 1,
        "totalCount": 2,
        "hasNextPage": False,
        "hasPreviousPage": False
    }

    import json
    page.route("**/api/admin/jobs**", lambda route: route.fulfill(
        status=200,
        content_type="application/json",
        body=json.dumps(mock_jobs)
    ))

    # Navigate to Batch Jobs page directly
    # Assuming the route is /jobs or accessible via sidebar.
    # Usually admin page defaults to dashboard, so we navigate to /jobs
    # Checking App.tsx would confirm routes, but let's try /jobs or click on sidebar.
    page.goto("http://localhost:5174/jobs")

    # Wait for the page to load
    try:
        page.wait_for_selector("h1:has-text('Batch Jobs')", timeout=10000)
    except Exception as e:
        print(f"Direct navigation failed, retrying via root. Error: {e}")
        # If direct navigation fails (e.g. client side routing issue on first load), go to root and navigate
        page.goto("http://localhost:5174/")
        page.click("a[href='/jobs']") # Assuming sidebar link
        page.wait_for_selector("h1:has-text('Batch Jobs')")

    print("Page loaded.")

    # verify search input exists
    page.wait_for_selector("input[placeholder='Search by target...']")
    print("Search input found.")

    # verify sort headers exist
    page.wait_for_selector("th:has-text('Job Definition')")
    page.wait_for_selector("th:has-text('Target')")
    page.wait_for_selector("th:has-text('Status')")
    page.wait_for_selector("th:has-text('Timestamp')")
    print("Sort headers found.")

    # Take screenshot of the list with search and sort
    page.screenshot(path="verification_list_v2.png")
    print("Screenshot verification_list_v2.png saved.")

    # Test Confirmation Dialog
    # Click "Ingest All Netherlands" button
    page.click("button:has-text('Ingest All Netherlands')")

    # Wait for dialog
    page.wait_for_selector("h3:has-text('Start Full Ingestion?')")
    print("Confirmation dialog appeared.")

    # Take screenshot of dialog
    page.screenshot(path="verification_dialog_v2.png")
    print("Screenshot verification_dialog_v2.png saved.")

    browser.close()

with sync_playwright() as p:
    run(p)
