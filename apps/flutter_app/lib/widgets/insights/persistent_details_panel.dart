import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:provider/provider.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_shadows.dart';
import '../../core/utils/map_utils.dart';
import '../../models/map_amenity.dart';
import '../../models/map_city_insight.dart';
import '../../providers/insights_provider.dart';

class PersistentDetailsPanel extends StatelessWidget {
  const PersistentDetailsPanel({super.key});

  @override
  Widget build(BuildContext context) {
    return Selector<InsightsProvider, Object?>(
      selector: (_, p) => p.selectedFeature,
      builder: (context, feature, _) {
        if (feature == null) return const SizedBox.shrink();

        if (feature is! MapCityInsight && feature is! MapAmenity) {
          debugPrint('PersistentDetailsPanel: Unsupported feature type: ${feature.runtimeType}');
          return const SizedBox.shrink();
        }

        return Positioned(
          left: 12,
          right: 12,
          bottom: 20,
          child: _PanelCard(feature: feature)
              .animate()
              .slideY(begin: 0.6, end: 0.0, duration: 340.ms, curve: Curves.easeOutQuint)
              .fadeIn(duration: 280.ms),
        );
      },
    );
  }
}

class _PanelCard extends StatelessWidget {
  final Object feature;
  const _PanelCard({required this.feature});

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    return Container(
      decoration: BoxDecoration(
        color: isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight,
        borderRadius: BorderRadius.circular(24),
        boxShadow: isDark ? ValoraShadows.lgDark : ValoraShadows.lg,
        border: Border.all(
          color: isDark ? ValoraColors.neutral700.withValues(alpha: 0.6) : ValoraColors.neutral200,
        ),
      ),
      child: Stack(
        children: [
          if (feature is MapCityInsight)
            _CityDetailsContent(city: feature as MapCityInsight)
          else if (feature is MapAmenity)
            _AmenityDetailsContent(amenity: feature as MapAmenity),
          Positioned(
            top: 10,
            right: 10,
            child: _CloseButton(),
          ),
        ],
      ),
    );
  }
}

class _CloseButton extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    return Material(
      color: Colors.transparent,
      child: IconButton(
        key: const Key('panel_close_button'),
        icon: Icon(
          Icons.close_rounded,
          size: 18,
          color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
        ),
        onPressed: () => context.read<InsightsProvider>().clearSelection(),
        style: IconButton.styleFrom(
          backgroundColor: isDark
              ? ValoraColors.neutral800.withValues(alpha: 0.6)
              : ValoraColors.neutral100,
          padding: const EdgeInsets.all(6),
          minimumSize: const Size(30, 30),
          maximumSize: const Size(30, 30),
        ),
      ),
    );
  }
}

// ---------------------------------------------------------------------------
// City Details
// ---------------------------------------------------------------------------

class _CityDetailsContent extends StatelessWidget {
  final MapCityInsight city;
  const _CityDetailsContent({required this.city});

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final compositeColor = MapUtils.getColorForScore(city.compositeScore);

    return Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // Gradient header strip
        Container(
          decoration: BoxDecoration(
            borderRadius: const BorderRadius.vertical(top: Radius.circular(24)),
            gradient: LinearGradient(
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
              colors: [
                compositeColor.withValues(alpha: isDark ? 0.22 : 0.12),
                compositeColor.withValues(alpha: isDark ? 0.08 : 0.04),
              ],
            ),
          ),
          padding: const EdgeInsets.fromLTRB(20, 18, 52, 16),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.center,
            children: [
              // Big score ring
              _ScoreRing(
                score: city.compositeScore,
                color: compositeColor,
                size: 58,
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      city.city,
                      style: Theme.of(context).textTheme.titleLarge?.copyWith(
                        fontWeight: FontWeight.w800,
                        letterSpacing: -0.4,
                      ),
                    ),
                    const SizedBox(height: 3),
                    Row(
                      children: [
                        Icon(
                          Icons.data_usage_rounded,
                          size: 11,
                          color: isDark ? ValoraColors.neutral500 : ValoraColors.neutral400,
                        ),
                        const SizedBox(width: 4),
                        Text(
                          '${city.count} data points',
                          style: Theme.of(context).textTheme.bodySmall?.copyWith(fontSize: 11.5),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),

        // Score grid
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 18),
          child: Column(
            children: [
              Row(
                children: [
                  Expanded(
                    child: _ScoreCard(
                      label: 'Safety',
                      icon: Icons.shield_rounded,
                      score: city.safetyScore,
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: _ScoreCard(
                      label: 'Social',
                      icon: Icons.people_rounded,
                      score: city.socialScore,
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: _ScoreCard(
                      label: 'Amenities',
                      icon: Icons.storefront_rounded,
                      score: city.amenitiesScore,
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ],
    );
  }
}

// ---------------------------------------------------------------------------
// Amenity Details
// ---------------------------------------------------------------------------

class _AmenityDetailsContent extends StatelessWidget {
  final MapAmenity amenity;
  const _AmenityDetailsContent({required this.amenity});

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    return Padding(
      padding: const EdgeInsets.fromLTRB(20, 20, 52, 20),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                width: 44,
                height: 44,
                decoration: BoxDecoration(
                  color: ValoraColors.primary.withValues(alpha: isDark ? 0.2 : 0.1),
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: ValoraColors.primary.withValues(alpha: 0.25)),
                ),
                child: Icon(
                  MapUtils.getAmenityIcon(amenity.type),
                  color: ValoraColors.primary,
                  size: 22,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      amenity.name,
                      style: Theme.of(context).textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 3),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                      decoration: BoxDecoration(
                        color: ValoraColors.primary.withValues(alpha: isDark ? 0.15 : 0.08),
                        borderRadius: BorderRadius.circular(20),
                      ),
                      child: Text(
                        amenity.type.toUpperCase(),
                        style: TextStyle(
                          fontSize: 10,
                          fontWeight: FontWeight.w700,
                          letterSpacing: 0.8,
                          color: ValoraColors.primary,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
          if (amenity.metadata != null && amenity.metadata!.isNotEmpty) ...[
            const SizedBox(height: 14),
            Divider(
              height: 1,
              color: isDark ? ValoraColors.neutral700 : ValoraColors.neutral200,
            ),
            const SizedBox(height: 10),
            ...amenity.metadata!.entries.take(5).map((e) {
              return Padding(
                padding: const EdgeInsets.only(bottom: 6),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(
                      e.key,
                      style: TextStyle(
                        fontSize: 12.5,
                        color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
                      ),
                    ),
                    Text(
                      e.value.toString(),
                      style: const TextStyle(
                        fontSize: 12.5,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ],
                ),
              );
            }),
          ],
        ],
      ),
    );
  }
}

// ---------------------------------------------------------------------------
// Small score card used in the city panel grid
// ---------------------------------------------------------------------------

class _ScoreCard extends StatelessWidget {
  final String label;
  final IconData icon;
  final double? score;

  const _ScoreCard({required this.label, required this.icon, required this.score});

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final color = MapUtils.getColorForScore(score);

    return Container(
      padding: const EdgeInsets.symmetric(vertical: 10, horizontal: 10),
      decoration: BoxDecoration(
        color: color.withValues(alpha: isDark ? 0.12 : 0.07),
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: color.withValues(alpha: 0.2)),
      ),
      child: Column(
        children: [
          Icon(icon, size: 16, color: color),
          const SizedBox(height: 5),
          Text(
            score != null ? score!.round().toString() : '–',
            style: TextStyle(
              fontSize: 18,
              fontWeight: FontWeight.w800,
              color: color,
              height: 1,
            ),
          ),
          const SizedBox(height: 3),
          Text(
            label,
            style: Theme.of(context).textTheme.labelSmall?.copyWith(
              fontSize: 10,
              color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
            ),
            textAlign: TextAlign.center,
          ),
        ],
      ),
    );
  }
}

// ---------------------------------------------------------------------------
// Circular score ring (arc) widget
// ---------------------------------------------------------------------------

class _ScoreRing extends StatelessWidget {
  final double? score;
  final Color color;
  final double size;

  const _ScoreRing({required this.score, required this.color, required this.size});

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final fraction = score != null ? (score! / 100).clamp(0.0, 1.0) : 0.0;

    return SizedBox(
      width: size,
      height: size,
      child: CustomPaint(
        painter: _RingPainter(
          fraction: fraction,
          color: color,
          trackColor: isDark ? ValoraColors.neutral700 : ValoraColors.neutral200,
        ),
        child: Center(
          child: Text(
            score != null ? score!.round().toString() : '–',
            style: TextStyle(
              fontSize: size * 0.28,
              fontWeight: FontWeight.w800,
              color: color,
              height: 1,
            ),
          ),
        ),
      ),
    );
  }
}

class _RingPainter extends CustomPainter {
  final double fraction;
  final Color color;
  final Color trackColor;

  const _RingPainter({
    required this.fraction,
    required this.color,
    required this.trackColor,
  });

  @override
  void paint(Canvas canvas, Size size) {
    final center = Offset(size.width / 2, size.height / 2);
    final radius = (size.width / 2) - 4;
    const startAngle = -math.pi / 2; // top
    const sweepFull = 2 * math.pi;

    final trackPaint = Paint()
      ..color = trackColor
      ..strokeWidth = 5
      ..style = PaintingStyle.stroke
      ..strokeCap = StrokeCap.round;

    final arcPaint = Paint()
      ..color = color
      ..strokeWidth = 5
      ..style = PaintingStyle.stroke
      ..strokeCap = StrokeCap.round;

    canvas.drawArc(
      Rect.fromCircle(center: center, radius: radius),
      startAngle,
      sweepFull,
      false,
      trackPaint,
    );

    if (fraction > 0) {
      canvas.drawArc(
        Rect.fromCircle(center: center, radius: radius),
        startAngle,
        sweepFull * fraction,
        false,
        arcPaint,
      );
    }
  }

  @override
  bool shouldRepaint(_RingPainter old) =>
      old.fraction != fraction || old.color != color || old.trackColor != trackColor;
}
