import 'dart:math' as math;
import 'package:flutter/material.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_typography.dart';

/// An animated circular gauge displaying a score from 0-100
/// with gradient coloring and smooth animation.
class ScoreGauge extends StatelessWidget {
  const ScoreGauge({
    super.key,
    required this.score,
    this.size = 180,
    this.strokeWidth = 12,
    this.label = 'Overall Score',
    this.animationDuration = const Duration(milliseconds: 1200),
  });

  final double score;
  final double size;
  final double strokeWidth;
  final String label;
  final Duration animationDuration;

  Color _getScoreColor(double score) {
    if (score >= 80) return ValoraColors.scoreExcellent;
    if (score >= 60) return ValoraColors.scoreGood;
    if (score >= 40) return ValoraColors.scoreAverage;
    return ValoraColors.scorePoor;
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scoreColor = _getScoreColor(score);
    final isDark = theme.brightness == Brightness.dark;

    return TweenAnimationBuilder<double>(
      tween: Tween(begin: 0, end: score.clamp(0, 100)),
      duration: animationDuration,
      curve: Curves.easeOutCubic,
      builder: (context, animatedScore, child) {
        return SizedBox(
          width: size,
          height: size,
          child: Stack(
            alignment: Alignment.center,
            children: [
              // Background track
              CustomPaint(
                size: Size(size, size),
                painter: _GaugePainter(
                  score: 100,
                  strokeWidth: strokeWidth,
                  color: isDark
                      ? ValoraColors.neutral800
                      : ValoraColors.neutral200,
                ),
              ),
              // Animated score arc
              CustomPaint(
                size: Size(size, size),
                painter: _GaugePainter(
                  score: animatedScore,
                  strokeWidth: strokeWidth,
                  color: scoreColor,
                  hasGradient: true,
                ),
              ),
              // Center content
              Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    animatedScore.round().toString(),
                    style: ValoraTypography.scoreDisplay.copyWith(
                      color: scoreColor,
                      fontSize: size * 0.28, // Responsive font size
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    label,
                    style: ValoraTypography.labelMedium.copyWith(
                      color: theme.colorScheme.onSurfaceVariant,
                    ),
                  ),
                ],
              ),
            ],
          ),
        );
      },
    );
  }
}

class _GaugePainter extends CustomPainter {
  _GaugePainter({
    required this.score,
    required this.strokeWidth,
    required this.color,
    this.hasGradient = false,
  });

  final double score;
  final double strokeWidth;
  final Color color;
  final bool hasGradient;

  @override
  void paint(Canvas canvas, Size size) {
    final center = Offset(size.width / 2, size.height / 2);
    final radius = (size.width - strokeWidth) / 2;

    // Start at 135 degrees (bottom left)
    final startAngle = math.pi * 0.75;

    // Sweep 270 degrees max (3/4 circle)
    final maxSweep = math.pi * 1.5;
    final sweepAngle = (score / 100) * maxSweep;

    final paint = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = strokeWidth
      ..strokeCap = StrokeCap.round;

    if (hasGradient) {
      // Create a gradient that follows the arc
      paint.shader = SweepGradient(
        startAngle: startAngle,
        endAngle: startAngle + maxSweep,
        colors: [
          color.withValues(alpha: 0.6),
          color,
        ],
        // Rotate gradient to match start angle
        transform: GradientRotation(startAngle - (math.pi * 0.05)),
      ).createShader(Rect.fromCircle(center: center, radius: radius));
    } else {
      paint.color = color;
    }

    canvas.drawArc(
      Rect.fromCircle(center: center, radius: radius),
      startAngle,
      sweepAngle,
      false,
      paint,
    );
  }

  @override
  bool shouldRepaint(_GaugePainter oldDelegate) =>
      score != oldDelegate.score ||
      color != oldDelegate.color ||
      strokeWidth != oldDelegate.strokeWidth ||
      hasGradient != oldDelegate.hasGradient;
}
