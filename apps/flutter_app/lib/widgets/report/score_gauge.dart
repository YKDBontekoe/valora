import 'dart:math' as math;
import 'package:flutter/material.dart';

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
    if (score >= 80) return const Color(0xFF10B981); // Emerald
    if (score >= 60) return const Color(0xFF3B82F6); // Blue
    if (score >= 40) return const Color(0xFFF59E0B); // Amber
    return const Color(0xFFEF4444); // Red
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scoreColor = _getScoreColor(score);
    
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
                  color: theme.colorScheme.surfaceContainerHighest.withValues(alpha: 0.5),
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
                    style: theme.textTheme.displaySmall?.copyWith(
                      fontWeight: FontWeight.bold,
                      color: scoreColor,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    label,
                    style: theme.textTheme.labelMedium?.copyWith(
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
    final startAngle = math.pi * 0.75;
    final sweepAngle = (score / 100) * math.pi * 1.5;

    final paint = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = strokeWidth
      ..strokeCap = StrokeCap.round;

    if (hasGradient) {
      paint.shader = SweepGradient(
        startAngle: startAngle,
        endAngle: startAngle + math.max(0.001, sweepAngle),
        colors: [
          color.withValues(alpha: 0.6),
          color,
        ],
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
