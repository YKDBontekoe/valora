import requests
import xml.etree.ElementTree as ET

def check_wfs(url, name):
    print(f"\n--- Checking {name} ---")
    try:
        response = requests.get(url + "?request=GetCapabilities&service=WFS", timeout=5)
        if response.status_code == 200:
            print(f"Success! {url}")
            # Parse XML to find feature types
            root = ET.fromstring(response.content)
            # Namespace map usually needed, but let's just crude search
            layers = []
            for elem in root.iter():
                if 'Name' in elem.tag and elem.text:
                    layers.append(elem.text)
            print(f"Found {len(layers)} layers. First 5: {layers[:5]}")
        else:
            print(f"Failed: {response.status_code}")
    except Exception as e:
        print(f"Error: {e}")

# 1. Bodemkaart (Soil Map) - Foundation Risk
check_wfs("https://service.pdok.nl/bzk/bro-bodemkaart/wfs/v1_0", "Bodemkaart")

# 2. 3D BAG - Solar Potential (Roof data)
# Note: 3D BAG URL might vary, trying a common one
check_wfs("https://data.3dbag.nl/api/BAG3D_v2/wfs", "3D BAG")

# 3. BAG (Basic) - Fallback for roof area
check_wfs("https://service.pdok.nl/lv/bag/wfs/v2_0", "BAG")
