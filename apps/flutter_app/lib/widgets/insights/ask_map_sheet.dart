import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/theme/valora_colors.dart';
import '../../providers/insights_provider.dart';

class AskMapSheet extends StatefulWidget {
  const AskMapSheet({super.key});

  @override
  State<AskMapSheet> createState() => _AskMapSheetState();
}

class _AskMapSheetState extends State<AskMapSheet> {
  final TextEditingController _controller = TextEditingController();

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  void _submit() {
    final query = _controller.text.trim();
    if (query.isNotEmpty) {
      // Get current map center/zoom if possible, or pass null
      // For now we just pass the query. Ideally we'd get context from map controller but provider handles state.
      // If provider has viewport state, use that.
      // But map controller is in the map widget.
      // We'll rely on the provider having a stored viewport if implemented, or just send query.
      context.read<InsightsProvider>().askMap(query);
      Navigator.pop(context);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.only(
        bottom: MediaQuery.of(context).viewInsets.bottom + 24,
        top: 24,
        left: 24,
        right: 24,
      ),
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surface,
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'Ask the Map',
            style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                  fontWeight: FontWeight.bold,
                ),
          ),
          const SizedBox(height: 8),
          Text(
            'Find neighborhoods by description, like "safe areas with good schools" or "lively places near parks".',
            style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                  color: Theme.of(context).textTheme.bodySmall?.color,
                ),
          ),
          const SizedBox(height: 24),
          TextField(
            controller: _controller,
            autofocus: true,
            decoration: InputDecoration(
              hintText: 'Describe what you are looking for...',
              filled: true,
              fillColor: Theme.of(context).colorScheme.surfaceContainerHighest.withOpacity(0.3),
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(16),
                borderSide: BorderSide.none,
              ),
              contentPadding: const EdgeInsets.all(16),
              suffixIcon: IconButton(
                icon: const Icon(Icons.send_rounded, color: ValoraColors.primary),
                onPressed: _submit,
              ),
            ),
            onSubmitted: (_) => _submit(),
          ),
        ],
      ),
    );
  }
}
