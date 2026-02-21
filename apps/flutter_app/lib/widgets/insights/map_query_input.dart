import 'package:flutter/material.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_shadows.dart';

class MapQueryInput extends StatefulWidget {
  final Function(String) onQuery;
  final bool isLoading;

  const MapQueryInput({
    super.key,
    required this.onQuery,
    this.isLoading = false,
  });

  @override
  State<MapQueryInput> createState() => _MapQueryInputState();
}

class _MapQueryInputState extends State<MapQueryInput> {
  final TextEditingController _controller = TextEditingController();
  final FocusNode _focusNode = FocusNode();

  @override
  void dispose() {
    _controller.dispose();
    _focusNode.dispose();
    super.dispose();
  }

  void _submit() {
    final text = _controller.text.trim();
    if (text.isNotEmpty) {
      widget.onQuery(text);
      _focusNode.unfocus();
    }
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return DecoratedBox(
      decoration: BoxDecoration(
        color: isDark ? ValoraColors.glassBlackStrong : ValoraColors.glassWhiteStrong,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(
          color: isDark ? ValoraColors.neutral700 : ValoraColors.neutral200,
        ),
        boxShadow: isDark ? ValoraShadows.mdDark : ValoraShadows.md,
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
        child: Row(
          children: [
            Container(
              width: 36,
              height: 36,
              decoration: BoxDecoration(
                color: ValoraColors.primaryLighter,
                borderRadius: BorderRadius.circular(10),
              ),
              child: const Icon(
                Icons.auto_awesome, // Changed to auto_awesome for "AI" feel
                color: ValoraColors.primary,
                size: 20,
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: TextField(
                controller: _controller,
                focusNode: _focusNode,
                decoration: const InputDecoration(
                  hintText: 'Ask the map (e.g., "Cheap safe areas")',
                  border: InputBorder.none,
                  isDense: true,
                  contentPadding: EdgeInsets.zero,
                ),
                style: Theme.of(context).textTheme.bodyMedium,
                textInputAction: TextInputAction.search,
                onSubmitted: (_) => _submit(),
                enabled: !widget.isLoading,
              ),
            ),
            const SizedBox(width: 8),
            if (widget.isLoading)
              const SizedBox(
                width: 20,
                height: 20,
                child: CircularProgressIndicator(strokeWidth: 2),
              )
            else
              InkWell(
                onTap: _submit,
                borderRadius: BorderRadius.circular(20),
                child: Container(
                  padding: const EdgeInsets.all(8),
                  decoration: BoxDecoration(
                    color: ValoraColors.primary,
                    shape: BoxShape.circle,
                  ),
                  child: const Icon(
                    Icons.arrow_upward_rounded,
                    color: Colors.white,
                    size: 18,
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}
