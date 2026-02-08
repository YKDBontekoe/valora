import 'dart:math' as math;
import 'package:flutter/material.dart';

/// A radar chart displaying category scores with animated drawing.
class CategoryRadar extends StatelessWidget {
  const CategoryRadar({
    super.key,
    required this.categoryScores,
    this.size = 200,
    this.animationDuration = const Duration(milliseconds: 1000),
  });

  final Map<String, double> categoryScores;
  final double size;
  final Duration animationDuration;

  @override
  Widget build(BuildContext context) {
    if (categoryScores.isEmpty) {
      return SizedBox(
        width: size,
        height: size,
        child: const Center(child: Text('No data')),
      );
    }

    final theme = Theme.of(context);
    final entries = categoryScores.entries.toList();

    return TweenAnimationBuilder<double>(
      tween: Tween(begin: 0, end: 1),
      duration: animationDuration,
      curve: Curves.easeOutCubic,
      builder: (context, progress, child) {
        return SizedBox(
          width: size,
          height: size,
          child: CustomPaint(
            painter: _RadarPainter(
              entries: entries,
              progress: progress,
              primaryColor: theme.colorScheme.primary,
              surfaceColor: theme.colorScheme.surfaceContainerHighest,
              labelColor: theme.colorScheme.onSurfaceVariant,
            ),
          ),
        );
      },
    );
  }
}

class _RadarPainter extends CustomPainter {
  _RadarPainter({
    required this.entries,
    required this.progress,
    required this.primaryColor,
    required this.surfaceColor,
    required this.labelColor,
  });

  final List<MapEntry<String, double>> entries;
  final double progress;
  final Color primaryColor;
  final Color surfaceColor;
  final Color labelColor;

  @override
  void paint(Canvas canvas, Size size) {
    final center = Offset(size.width / 2, size.height / 2);
    final maxRadius = (size.width / 2) * 0.7;
    final labelRadius = (size.width / 2) * 0.92;
    final count = entries.length;
    final angleStep = (2 * math.pi) / count;
    final startAngle = -math.pi / 2;

    // Draw grid circles
    final gridPaint = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1
      ..color = surfaceColor;

    for (int i = 1; i <= 5; i++) {
      canvas.drawCircle(center, maxRadius * i / 5, gridPaint);
    }

    // Draw axis lines
    for (int i = 0; i < count; i++) {
      final angle = startAngle + angleStep * i;
      final endPoint = Offset(
        center.dx + maxRadius * math.cos(angle),
        center.dy + maxRadius * math.sin(angle),
      );
      canvas.drawLine(center, endPoint, gridPaint);
    }

    // Build polygon points
    final points = <Offset>[];
    for (int i = 0; i < count; i++) {
      final angle = startAngle + angleStep * i;
      final value = (entries[i].value / 100).clamp(0.0, 1.0) * progress;
      final radius = maxRadius * value;
      points.add(Offset(
        center.dx + radius * math.cos(angle),
        center.dy + radius * math.sin(angle),
      ));
    }

    // Draw filled polygon
    if (points.isNotEmpty) {
      final path = Path()..moveTo(points[0].dx, points[0].dy);
      for (int i = 1; i < points.length; i++) {
        path.lineTo(points[i].dx, points[i].dy);
      }
      path.close();

      final fillPaint = Paint()
        ..style = PaintingStyle.fill
        ..color = primaryColor.withValues(alpha: 0.2);
      canvas.drawPath(path, fillPaint);

      final strokePaint = Paint()
        ..style = PaintingStyle.stroke
        ..strokeWidth = 2.5
        ..color = primaryColor;
      canvas.drawPath(path, strokePaint);

      // Draw points
      final pointPaint = Paint()
        ..style = PaintingStyle.fill
        ..color = primaryColor;
      for (final point in points) {
        canvas.drawCircle(point, 4, pointPaint);
      }
    }

    // Draw labels
    final textPainter = TextPainter(textDirection: TextDirection.ltr);
    for (int i = 0; i < count; i++) {
      final angle = startAngle + angleStep * i;
      final labelPoint = Offset(
        center.dx + labelRadius * math.cos(angle),
        center.dy + labelRadius * math.sin(angle),
      );

      textPainter.text = TextSpan(
        text: entries[i].key,
        style: TextStyle(
          color: labelColor,
          fontSize: 11,
          fontWeight: FontWeight.w500,
        ),
      );
      textPainter.layout();

      // Center the text around the point
      final labelOffset = Offset(
        labelPoint.dx - textPainter.width / 2,
        labelPoint.dy - textPainter.height / 2,
      );
      textPainter.paint(canvas, labelOffset);
    }
  }

  @override
  bool shouldRepaint(_RadarPainter oldDelegate) =>
      progress != oldDelegate.progress || entries != oldDelegate.entries;
}
