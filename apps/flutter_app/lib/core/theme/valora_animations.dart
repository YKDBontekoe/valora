import 'package:flutter/material.dart';

/// Valora Design System - Animation Tokens (v2)
///
/// Refined motion system with spring-physics curves and staggered helpers.
abstract final class ValoraAnimations {
  // ============================================
  // DURATIONS
  // ============================================

  /// Instant - 80ms
  /// Use for: Micro-interactions, tab switches, opacity toggles
  static const Duration instant = Duration(milliseconds: 80);

  /// Fast - 150ms
  /// Use for: Button presses, hover states, small toggles
  static const Duration fast = Duration(milliseconds: 150);

  /// Normal - 250ms
  /// Use for: Simple transitions, fades, color changes
  static const Duration normal = Duration(milliseconds: 250);

  /// Medium - 350ms
  /// Use for: Content transitions, slide-ins, layout changes
  static const Duration medium = Duration(milliseconds: 350);

  /// Slow - 500ms
  /// Use for: Large element movements, entrance animations
  static const Duration slow = Duration(milliseconds: 500);

  /// Very slow - 800ms
  /// Use for: Background effects, loading shimmers, dramatic entrances
  static const Duration verySlow = Duration(milliseconds: 800);

  /// Extra slow - 1200ms
  /// Use for: Background glow pulses, ambient effects
  static const Duration extraSlow = Duration(milliseconds: 1200);

  // ============================================
  // CURVES
  // ============================================

  /// Standard — smooth ease-in-out for general transitions
  static const Curve standard = Curves.easeInOutCubic;

  /// Emphatic — bouncy overshoot for selection/success
  static const Curve emphatic = Curves.easeOutBack;

  /// Deceleration — smooth landing for entering elements
  static const Curve deceleration = Curves.easeOutCubic;

  /// Acceleration — smooth exit for leaving elements
  static const Curve acceleration = Curves.easeInCubic;

  /// Spring — natural spring physics feel
  static const Curve spring = Curves.elasticOut;

  /// Smooth — very gentle ease for subtle transitions
  static const Curve smooth = Curves.easeOutQuart;

  /// Snappy — quick, responsive feel
  static const Curve snappy = Curves.easeOutExpo;

  // ============================================
  // STAGGER DELAYS
  // ============================================

  /// Stagger interval for list item entrances
  static const Duration staggerInterval = Duration(milliseconds: 50);

  /// Short stagger for small groups
  static const Duration staggerShort = Duration(milliseconds: 30);

  /// Long stagger for large content blocks
  static const Duration staggerLong = Duration(milliseconds: 80);
}
