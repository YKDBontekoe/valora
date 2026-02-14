import 'package:flutter/material.dart';
import 'valora_colors.dart';

/// Valora Design System - Shadow Tokens (v2)
///
/// Layered shadows for realistic depth, colored glows for emphasis,
/// and inner shadows for inset effects.
abstract final class ValoraShadows {
  // ============================================
  // LIGHT MODE SHADOWS (Layered for realism)
  // ============================================

  /// Small shadow — Subtle lift
  static final List<BoxShadow> sm = [
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.03),
      blurRadius: 1,
      offset: const Offset(0, 1),
    ),
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.04),
      blurRadius: 3,
      offset: const Offset(0, 2),
    ),
  ];

  /// Medium shadow — Cards, panels
  static final List<BoxShadow> md = [
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.03),
      blurRadius: 2,
      offset: const Offset(0, 1),
    ),
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.05),
      blurRadius: 8,
      offset: const Offset(0, 4),
    ),
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.02),
      blurRadius: 16,
      offset: const Offset(0, 8),
      spreadRadius: -4,
    ),
  ];

  /// Large shadow — Hover states, prominent cards
  static final List<BoxShadow> lg = [
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.04),
      blurRadius: 4,
      offset: const Offset(0, 2),
    ),
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.06),
      blurRadius: 16,
      offset: const Offset(0, 8),
      spreadRadius: -2,
    ),
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.04),
      blurRadius: 32,
      offset: const Offset(0, 16),
      spreadRadius: -8,
    ),
  ];

  /// Extra large shadow — Modals, floating nav
  static final List<BoxShadow> xl = [
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.06),
      blurRadius: 8,
      offset: const Offset(0, 4),
    ),
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.1),
      blurRadius: 32,
      offset: const Offset(0, 16),
      spreadRadius: -4,
    ),
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.06),
      blurRadius: 64,
      offset: const Offset(0, 32),
      spreadRadius: -16,
    ),
  ];

  // ============================================
  // DARK MODE SHADOWS (Deeper, richer)
  // ============================================

  static final List<BoxShadow> smDark = [
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.2),
      blurRadius: 2,
      offset: const Offset(0, 1),
    ),
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.15),
      blurRadius: 4,
      offset: const Offset(0, 2),
    ),
  ];

  static final List<BoxShadow> mdDark = [
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.25),
      blurRadius: 4,
      offset: const Offset(0, 2),
    ),
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.2),
      blurRadius: 12,
      offset: const Offset(0, 6),
      spreadRadius: -2,
    ),
  ];

  static final List<BoxShadow> lgDark = [
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.35),
      blurRadius: 12,
      offset: const Offset(0, 6),
      spreadRadius: -2,
    ),
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.2),
      blurRadius: 32,
      offset: const Offset(0, 16),
      spreadRadius: -8,
    ),
  ];

  static final List<BoxShadow> xlDark = [
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.45),
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

  // ============================================
  // COLORED GLOW SHADOWS
  // ============================================

  /// Primary glow — for primary interactive elements
  static final List<BoxShadow> primary = [
    BoxShadow(
      color: ValoraColors.primary.withValues(alpha: 0.2),
      blurRadius: 16,
      offset: const Offset(0, 4),
      spreadRadius: -2,
    ),
    BoxShadow(
      color: ValoraColors.primary.withValues(alpha: 0.1),
      blurRadius: 32,
      offset: const Offset(0, 8),
      spreadRadius: -4,
    ),
  ];

  /// Primary glow (dark) — more vivid glow for dark mode
  static final List<BoxShadow> primaryDark = [
    BoxShadow(
      color: ValoraColors.primaryLight.withValues(alpha: 0.15),
      blurRadius: 20,
      offset: const Offset(0, 4),
      spreadRadius: -2,
    ),
    BoxShadow(
      color: ValoraColors.primaryLight.withValues(alpha: 0.08),
      blurRadius: 40,
      offset: const Offset(0, 8),
      spreadRadius: -4,
    ),
  ];

  /// Success glow
  static final List<BoxShadow> successGlow = [
    BoxShadow(
      color: ValoraColors.success.withValues(alpha: 0.2),
      blurRadius: 12,
      offset: const Offset(0, 4),
    ),
  ];

  /// Error glow
  static final List<BoxShadow> errorGlow = [
    BoxShadow(
      color: ValoraColors.error.withValues(alpha: 0.2),
      blurRadius: 12,
      offset: const Offset(0, 4),
    ),
  ];

  // ============================================
  // INNER SHADOWS (for inset effects)
  // ============================================

  /// Subtle inset shadow for depth on light surfaces
  static final List<BoxShadow> innerLight = [
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.04),
      blurRadius: 4,
      offset: const Offset(0, 2),
      blurStyle: BlurStyle.inner,
    ),
  ];

  /// Subtle inset shadow for dark surfaces
  static final List<BoxShadow> innerDark = [
    BoxShadow(
      color: Colors.black.withValues(alpha: 0.2),
      blurRadius: 4,
      offset: const Offset(0, 2),
      blurStyle: BlurStyle.inner,
    ),
  ];
}
