import 'package:flutter/material.dart';

class ValoraSlider extends StatelessWidget {
  const ValoraSlider({
    super.key,
    required this.value,
    required this.onChanged,
    this.min = 0.0,
    this.max = 1.0,
    this.divisions,
    this.label,
  });

  final double value;
  final ValueChanged<double>? onChanged;
  final double min;
  final double max;
  final int? divisions;
  final String? label;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final primaryColor = theme.colorScheme.primary;

    return SliderTheme(
      data: SliderTheme.of(context).copyWith(
        trackHeight: 4,
        activeTrackColor: primaryColor,
        inactiveTrackColor: primaryColor.withValues(alpha: 0.1),
        thumbShape: const RoundSliderThumbShape(
          enabledThumbRadius: 8,
          pressedElevation: 6,
        ),
        overlayShape: const RoundSliderOverlayShape(overlayRadius: 20),
        overlayColor: primaryColor.withValues(alpha: 0.1),
        valueIndicatorShape: const PaddleSliderValueIndicatorShape(),
        valueIndicatorColor: primaryColor,
        valueIndicatorTextStyle: TextStyle(
          color: theme.colorScheme.onPrimary,
          fontWeight: FontWeight.bold,
        ),
      ),
      child: Slider(
        value: value,
        onChanged: onChanged,
        min: min,
        max: max,
        divisions: divisions,
        label: label,
      ),
    );
  }
}
