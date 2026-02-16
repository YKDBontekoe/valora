import 'dart:math' as math;
import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../../models/context_report.dart';

class ContextPieChart extends StatelessWidget {
  const ContextPieChart({
    super.key,
    required this.metrics,
    this.size = 160,
    this.holeRadiusPercent = 0.5,
  });

  final List<ContextMetric> metrics;
  final double size;
  final double holeRadiusPercent;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    // Filter out metrics with null or zero values
    final validMetrics = metrics.where((m) => (m.value ?? 0) > 0).toList();
    if (validMetrics.isEmpty) return const SizedBox.shrink();

    final total = validMetrics.fold<double>(0, (sum, e) => sum + (e.value ?? 0));
    final colors = [
      theme.colorScheme.primary,
      theme.colorScheme.secondary,
      theme.colorScheme.tertiary,
      theme.colorScheme.error,
      theme.colorScheme.primaryContainer,
      theme.colorScheme.secondaryContainer,
    ];

    return Row(
      children: [
        SizedBox(
          width: size,
          height: size,
          child: CustomPaint(
            painter: _PiePainter(
              metrics: validMetrics,
              total: total,
              colors: colors,
              holeRadiusPercent: holeRadiusPercent,
            ),
          ).animate().fadeIn(duration: 600.ms).scale(begin: const Offset(0.8, 0.8)),
        ),
        const SizedBox(width: 24),
        Expanded(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: validMetrics.asMap().entries.map((entry) {
              final index = entry.key;
              final metric = entry.value;
              final percentage = (metric.value ?? 0) / total * 100;

              return Padding(
                padding: const EdgeInsets.symmetric(vertical: 4),
                child: Row(
                  children: [
                    Container(
                      width: 12,
                      height: 12,
                      decoration: BoxDecoration(
                        color: colors[index % colors.length],
                        shape: BoxShape.circle,
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        metric.label,
                        style: theme.textTheme.bodySmall,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                    Text(
                      '${percentage.toStringAsFixed(0)}%',
                      style: theme.textTheme.labelSmall?.copyWith(fontWeight: FontWeight.bold),
                    ),
                  ],
                ),
              ).animate().fadeIn(delay: (index * 100).ms, duration: 400.ms).slideX(begin: 0.1);
            }).toList(),
          ),
        ),
      ],
    );
  }
}

class _PiePainter extends CustomPainter {
  _PiePainter({
    required this.metrics,
    required this.total,
    required this.colors,
    required this.holeRadiusPercent,
  });

  final List<ContextMetric> metrics;
  final double total;
  final List<Color> colors;
  final double holeRadiusPercent;

  @override
  void paint(Canvas canvas, Size size) {
    final center = Offset(size.width / 2, size.height / 2);
    final radius = size.width / 2;
    final rect = Rect.fromCircle(center: center, radius: radius);

    double startAngle = -math.pi / 2;

    for (int i = 0; i < metrics.length; i++) {
      final sweepAngle = ((metrics[i].value ?? 0) / total) * 2 * math.pi;

      final paint = Paint()
        ..style = PaintingStyle.fill
        ..color = colors[i % colors.length];

      canvas.drawArc(rect, startAngle, sweepAngle, true, paint);
      startAngle += sweepAngle;
    }

    // Draw hole for donut chart
    if (holeRadiusPercent > 0) {
      final holePaint = Paint()
        ..style = PaintingStyle.fill
        ..color = Colors.white; // Should ideally match background or be transparent with ClipPath

      // Use BlendMode to make it transparent if possible, but white is safer for now
      canvas.drawCircle(center, radius * holeRadiusPercent, holePaint);
    }

    // Draw white borders between slices
    final borderPaint = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = 2
      ..color = Colors.white;

    startAngle = -math.pi / 2;
    for (int i = 0; i < metrics.length; i++) {
      final sweepAngle = ((metrics[i].value ?? 0) / total) * 2 * math.pi;
      final lineEndPoint = Offset(
        center.dx + radius * math.cos(startAngle),
        center.dy + radius * math.sin(startAngle),
      );
      canvas.drawLine(center, lineEndPoint, borderPaint);
      startAngle += sweepAngle;
    }
  }

  @override
  bool shouldRepaint(_PiePainter oldDelegate) => true;
}
