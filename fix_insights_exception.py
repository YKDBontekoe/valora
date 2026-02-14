with open('apps/flutter_app/lib/screens/insights/insights_screen.dart', 'r') as f:
    content = f.read()

old_logic = """    if (!mounted || _mapController.camera == null) return const SizedBox.shrink();
    final zoom = _mapController.camera.zoom;"""

new_logic = """    if (!mounted) return const SizedBox.shrink();
    double zoom;
    try {
      zoom = _mapController.camera.zoom;
    } catch (_) {
      return const SizedBox.shrink();
    }"""

content = content.replace(old_logic, new_logic)

with open('apps/flutter_app/lib/screens/insights/insights_screen.dart', 'w') as f:
    f.write(content)
