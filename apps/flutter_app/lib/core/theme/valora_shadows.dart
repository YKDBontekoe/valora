import 'package:flutter/material.dart';
import 'valora_colors.dart';

/// Valora Design System - Shadow Tokens
///
/// Standardized shadows for depth and elevation.
abstract final class ValoraShadows {
  // ============================================
  // LIGHT MODE SHADOWS
  // ============================================

  /// Small shadow - Low elevation
  static final List<BoxShadow> sm = [
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.04),
      blurRadius: 2,
      offset: const Offset(0, 1),
    ),
  ];

  /// Medium shadow - Cards, standard elements
  static final List<BoxShadow> md = [
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.04),
      blurRadius: 4,
      offset: const Offset(0, 2),
    ),
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.02),
      blurRadius: 8,
      offset: const Offset(0, 4),
      spreadRadius: -2,
    ),
  ];

  /// Large shadow - Hover states, prominent cards
  static final List<BoxShadow> lg = [
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.08),
      blurRadius: 12,
      offset: const Offset(0, 6),
      spreadRadius: -2,
    ),
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.04),
      blurRadius: 24,
      offset: const Offset(0, 12),
      spreadRadius: -4,
    ),
  ];

  /// Extra large shadow - Modals, floating actions
  static final List<BoxShadow> xl = [
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.12),
      blurRadius: 24,
      offset: const Offset(0, 12),
      spreadRadius: -4,
    ),
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.08),
      blurRadius: 48,
      offset: const Offset(0, 24),
      spreadRadius: -12,
    ),
  ];

  /// Colored shadow for primary actions (Light)
  static final List<BoxShadow> primary = [
    BoxShadow(
      color: ValoraColors.primary.withValues(alpha: 0.25),
      blurRadius: 12,
      offset: const Offset(0, 6),
    ),
  ];

  // ============================================
  // DARK MODE SHADOWS
  // ============================================

  /// Small shadow (Dark)
  static final List<BoxShadow> smDark = [
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.2),
      blurRadius: 2,
      offset: const Offset(0, 1),
    ),
  ];

  /// Medium shadow (Dark)
  static final List<BoxShadow> mdDark = [
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.3),
      blurRadius: 4,
      offset: const Offset(0, 2),
    ),
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.2),
      blurRadius: 8,
      offset: const Offset(0, 4),
      spreadRadius: -2,
    ),
  ];

  /// Large shadow (Dark)
  static final List<BoxShadow> lgDark = [
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.4),
      blurRadius: 12,
      offset: const Offset(0, 6),
      spreadRadius: -2,
    ),
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.2),
      blurRadius: 24,
      offset: const Offset(0, 12),
      spreadRadius: -4,
    ),
  ];

  /// Extra large shadow (Dark)
  static final List<BoxShadow> xlDark = [
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.5),
      blurRadius: 24,
      offset: const Offset(0, 12),
      spreadRadius: -4,
    ),
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.3),
      blurRadius: 48,
      offset: const Offset(0, 24),
      spreadRadius: -12,
    ),
  ];
}
