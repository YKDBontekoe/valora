import sys

with open('apps/flutter_app/lib/screens/saved_listings_screen.dart', 'r') as f:
    content = f.read()

# Update ValoraTextField in SavedListingsScreen
search_pattern = "prefixIcon: Icons.search_rounded,"
replacement = search_pattern + """
                      suffixIcon: _searchController.text.isNotEmpty
                          ? IconButton(
                              icon: const Icon(Icons.clear_rounded),
                              onPressed: () => _searchController.clear(),
                            )
                          : null,"""
content = content.replace(search_pattern, replacement)

with open('apps/flutter_app/lib/screens/saved_listings_screen.dart', 'w') as f:
    f.write(content)
