import 'package:flutter/material.dart';

/// Valora Design System - Animation Tokens
///
/// Standardized animation durations and curves for consistent motion.
abstract final class ValoraAnimations {
  // ============================================
  // DURATIONS
  // ============================================

  /// Very fast - 100ms
  /// Use for: Micro-interactions, hover states, button presses
  static const Duration fast = Duration(milliseconds: 100);

  /// Fast - 200ms
  /// Use for: Small transitions, simple fades
  static const Duration normal = Duration(milliseconds: 200);

  /// Medium - 300ms
  /// Use for: Content transitions, large element movements
  static const Duration medium = Duration(milliseconds: 300);

  /// Slow - 400ms
  /// Use for: Entrance animations, complex transitions
  static const Duration slow = Duration(milliseconds: 400);

  /// Very slow - 800ms
  /// Use for: Background effects, loading shimmers
  static const Duration verySlow = Duration(milliseconds: 800);

  // ============================================
  // CURVES
  // ============================================

  /// Standard curve - easeInOut
  static const Curve standard = Curves.easeInOut;

  /// Emphatic curve - easeOutBack
  /// Use for: Popping elements, success states
  static const Curve emphatic = Curves.easeOutBack;

  /// Deceleration curve - easeOut
  /// Use for: Entering elements
  static const Curve deceleration = Curves.easeOut;

  /// Acceleration curve - easeIn
  /// Use for: Exiting elements
  static const Curve acceleration = Curves.easeIn;
}
