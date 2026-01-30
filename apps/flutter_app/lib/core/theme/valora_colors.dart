import 'package:flutter/material.dart';

/// Valora Design System - Color Tokens
///
/// A semantic color palette following Material Design 3 principles
/// with custom brand identity for the Valora real estate app.
abstract final class ValoraColors {
  // ============================================
  // BRAND COLORS
  // ============================================

  /// Primary brand color - Blue Violet
  static const Color primary = Color(0xFF6366F1);
  static const Color primaryLight = Color(0xFF818CF8);
  static const Color primaryDark = Color(0xFF4F46E5);

  /// Accent color - Warm Coral for CTAs and highlights
  static const Color accent = Color(0xFFF97316);
  static const Color accentLight = Color(0xFFFB923C);
  static const Color accentDark = Color(0xFFEA580C);

  // ============================================
  // NEUTRAL COLORS
  // ============================================

  static const Color neutral50 = Color(0xFFFAFAFA);
  static const Color neutral100 = Color(0xFFF5F5F5);
  static const Color neutral200 = Color(0xFFE5E5E5);
  static const Color neutral300 = Color(0xFFD4D4D4);
  static const Color neutral400 = Color(0xFFA3A3A3);
  static const Color neutral500 = Color(0xFF737373);
  static const Color neutral600 = Color(0xFF525252);
  static const Color neutral700 = Color(0xFF404040);
  static const Color neutral800 = Color(0xFF262626);
  static const Color neutral900 = Color(0xFF171717);

  // ============================================
  // SEMANTIC COLORS
  // ============================================

  /// Success - Green for positive states
  static const Color success = Color(0xFF22C55E);
  static const Color successLight = Color(0xFFDCFCE7);
  static const Color successDark = Color(0xFF16A34A);

  /// Warning - Amber for caution states
  static const Color warning = Color(0xFFF59E0B);
  static const Color warningLight = Color(0xFFFEF3C7);
  static const Color warningDark = Color(0xFFD97706);

  /// Error - Red for error states
  static const Color error = Color(0xFFEF4444);
  static const Color errorLight = Color(0xFFFEE2E2);
  static const Color errorDark = Color(0xFFDC2626);

  /// Info - Blue for informational states
  static const Color info = Color(0xFF3B82F6);
  static const Color infoLight = Color(0xFFDBEAFE);
  static const Color infoDark = Color(0xFF2563EB);

  // ============================================
  // SURFACE COLORS - LIGHT THEME
  // ============================================

  static const Color backgroundLight = Color(0xFFFAFAFA);
  static const Color surfaceLight = Color(0xFFFFFFFF);
  static const Color surfaceVariantLight = Color(0xFFF5F5F5);
  static const Color onBackgroundLight = Color(0xFF171717);
  static const Color onSurfaceLight = Color(0xFF262626);
  static const Color onSurfaceVariantLight = Color(0xFF525252);

  // ============================================
  // SURFACE COLORS - DARK THEME
  // ============================================

  static const Color backgroundDark = Color(0xFF0A0A0A);
  static const Color surfaceDark = Color(0xFF171717);
  static const Color surfaceVariantDark = Color(0xFF262626);
  static const Color onBackgroundDark = Color(0xFFFAFAFA);
  static const Color onSurfaceDark = Color(0xFFF5F5F5);
  static const Color onSurfaceVariantDark = Color(0xFFA3A3A3);

  // ============================================
  // REAL ESTATE SPECIFIC COLORS
  // ============================================

  /// Price highlight color
  static const Color priceTag = Color(0xFF059669);
  static const Color priceTagDark = Color(0xFF34D399);

  /// New listing badge
  static const Color newBadge = Color(0xFFEC4899);

  /// Sold/Under offer badge
  static const Color soldBadge = Color(0xFF6B7280);

  // ============================================
  // GRADIENTS
  // ============================================

  static const LinearGradient primaryGradient = LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    colors: [primary, primaryDark],
  );

  static const LinearGradient accentGradient = LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    colors: [accentLight, accent],
  );

  static const LinearGradient surfaceGradient = LinearGradient(
    begin: Alignment.topCenter,
    end: Alignment.bottomCenter,
    colors: [surfaceLight, neutral100],
  );
}
