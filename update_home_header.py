import sys

with open('apps/flutter_app/lib/widgets/home/home_header.dart', 'r') as f:
    content = f.read()

# Define the old block to replace
old_block = '''              Material(
                color: Colors.transparent,
                child: InkWell(
                  onTap: () {
                    HapticFeedback.lightImpact();
                    widget.onFilterPressed();
                  },
                  borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
                  child: Container(
                    width: 52,
                    height: 52,
                    decoration: BoxDecoration(
                      color: ValoraColors.primary.withValues(alpha: 0.1),
                      borderRadius: BorderRadius.circular(
                        ValoraSpacing.radiusLg,
                      ),
                      border: Border.all(
                        color: ValoraColors.primary.withValues(alpha: 0.2),
                      ),
                    ),
                    child: Stack(
                      alignment: Alignment.center,
                      children: [
                        const Icon(
                          Icons.tune_rounded,
                          color: ValoraColors.primary,
                        ),
                        if (widget.activeFilterCount > 0)
                          Positioned(
                            top: 12,
                            right: 12,
                            child: Container(
                              width: 8,
                              height: 8,
                              decoration: BoxDecoration(
                                color: ValoraColors.accent,
                                shape: BoxShape.circle,
                                border: Border.all(
                                  color: isDark
                                      ? ValoraColors.surfaceDark
                                      : ValoraColors.surfaceLight,
                                  width: 1.5,
                                ),
                              ),
                            ),
                          ).animate().scale(curve: Curves.elasticOut),
                      ],
                    ),
                  ),
                ),
              ),'''

# Define the new usage
new_usage = '''              _FilterButton(
                onPressed: widget.onFilterPressed,
                activeFilterCount: widget.activeFilterCount,
              ),'''

# Define the new class definition
new_class = '''

class _FilterButton extends StatefulWidget {
  final VoidCallback onPressed;
  final int activeFilterCount;

  const _FilterButton({required this.onPressed, required this.activeFilterCount});

  @override
  State<_FilterButton> createState() => _FilterButtonState();
}

class _FilterButtonState extends State<_FilterButton> {
  bool _isPressed = false;
  bool _isHovered = false;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return MouseRegion(
      onEnter: (_) => setState(() => _isHovered = true),
      onExit: (_) => setState(() => _isHovered = false),
      child: GestureDetector(
        onTapDown: (_) => setState(() => _isPressed = true),
        onTapUp: (_) => setState(() => _isPressed = false),
        onTapCancel: () => setState(() => _isPressed = false),
        onTap: () {
           HapticFeedback.lightImpact();
           widget.onPressed();
        },
        child: AnimatedContainer(
          duration: ValoraAnimations.fast,
          width: 52,
          height: 52,
          decoration: BoxDecoration(
            color: _isHovered
                ? ValoraColors.primary.withValues(alpha: 0.15)
                : ValoraColors.primary.withValues(alpha: 0.1),
            borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
            border: Border.all(
              color: ValoraColors.primary.withValues(alpha: 0.2),
            ),
          ),
          child: Stack(
            alignment: Alignment.center,
            children: [
              const Icon(
                Icons.tune_rounded,
                color: ValoraColors.primary,
              ),
              if (widget.activeFilterCount > 0)
                Positioned(
                  top: 12,
                  right: 12,
                  child: Container(
                    width: 8,
                    height: 8,
                    decoration: BoxDecoration(
                      color: ValoraColors.accent,
                      shape: BoxShape.circle,
                      border: Border.all(
                        color: isDark
                            ? ValoraColors.surfaceDark
                            : ValoraColors.surfaceLight,
                        width: 1.5,
                      ),
                    ),
                  ),
                ).animate().scale(curve: Curves.elasticOut),
            ],
          ),
        )
        .animate(target: _isPressed ? 1 : 0)
        .scale(end: const Offset(0.95, 0.95), duration: 100.ms),
      ),
    );
  }
}
'''

if old_block in content:
    content = content.replace(old_block, new_usage)
    content += new_class

    with open('apps/flutter_app/lib/widgets/home/home_header.dart', 'w') as f:
        f.write(content)
    print("Successfully updated HomeHeader")
else:
    print("Could not find block to replace")
