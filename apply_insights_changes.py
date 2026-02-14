import sys

with open('apps/flutter_app/lib/screens/insights/insights_screen.dart', 'r') as f:
    content = f.read()

# Update onPositionChanged
content = content.replace(
    'if (hasGesture) _onMapChanged();',
    'if (hasGesture) { _onMapChanged(); setState(() {}); }'
)

# Add Legend and Zoom Warning to Stack
stack_end = '_buildLayerToggle(context, provider),'
replacement = stack_end + """
              if (provider.showOverlays)
                Positioned(
                  left: 16,
                  bottom: 24,
                  child: MapLegend(metric: provider.selectedOverlayMetric),
                ),
              _buildZoomWarning(provider),"""
content = content.replace(stack_end, replacement)

# Add _buildZoomWarning method before the end of the class
class_end = '  String _getOverlayLabel(MapOverlayMetric metric) {'
zoom_warning_method = """
  Widget _buildZoomWarning(InsightsProvider provider) {
    if (!mounted || _mapController.camera == null) return const SizedBox.shrink();
    final zoom = _mapController.camera.zoom;
    final needsZoomForAmenities = provider.showAmenities && zoom < 13;
    final needsZoomForOverlays = provider.showOverlays && zoom < 11;

    if (!needsZoomForAmenities && !needsZoomForOverlays) return const SizedBox.shrink();

    return Positioned(
      top: 110,
      left: 16,
      right: 16,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
        decoration: BoxDecoration(
          color: Colors.amber.withValues(alpha: 0.9),
          borderRadius: BorderRadius.circular(12),
          boxShadow: const [BoxShadow(color: Colors.black12, blurRadius: 4)],
        ),
        child: Row(
          children: [
            const Icon(Icons.zoom_in_rounded, size: 20, color: Colors.black87),
            const SizedBox(width: 12),
            Expanded(
              child: Text(
                needsZoomForAmenities
                  ? 'Zoom in further to see amenities'
                  : 'Zoom in further to see overlays',
                style: const TextStyle(
                  color: Colors.black87,
                  fontSize: 12,
                  fontWeight: FontWeight.w500,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

"""
content = content.replace(class_end, zoom_warning_method + class_end)

with open('apps/flutter_app/lib/screens/insights/insights_screen.dart', 'w') as f:
    f.write(content)
