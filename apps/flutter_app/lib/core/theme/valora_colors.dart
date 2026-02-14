import 'package:flutter/material.dart';

/// Valora Design System - Color Tokens (v2)
///
/// A refined, premium color palette with deeper tones, sophisticated gradients,
/// and a complete surface container system following Material Design 3.
abstract final class ValoraColors {
  // ============================================
  // BRAND COLORS — Deep Indigo / Electric Violet
  // ============================================

  /// Primary — saturated indigo with electric undertones
  static const Color primary = Color(0xFF6366F1);
  static const Color primaryLight = Color(0xFF818CF8);
  static const Color primaryLighter = Color(0xFFA5B4FC);
  static const Color primaryDark = Color(0xFF4F46E5);
  static const Color primaryDarker = Color(0xFF3730A3);

  /// Accent — warm amber/coral for CTAs and highlights
  static const Color accent = Color(0xFFF97316);
  static const Color accentLight = Color(0xFFFB923C);
  static const Color accentLighter = Color(0xFFFDBA74);
  static const Color accentDark = Color(0xFFEA580C);
  static const Color accentDarker = Color(0xFFC2410C);

  // ============================================
  // NEUTRAL COLORS — Slate-based for premium feel
  // ============================================

  static const Color neutral50 = Color(0xFFF8FAFC);
  static const Color neutral100 = Color(0xFFF1F5F9);
  static const Color neutral200 = Color(0xFFE2E8F0);
  static const Color neutral300 = Color(0xFFCBD5E1);
  static const Color neutral400 = Color(0xFF94A3B8);
  static const Color neutral500 = Color(0xFF64748B);
  static const Color neutral600 = Color(0xFF475569);
  static const Color neutral700 = Color(0xFF334155);
  static const Color neutral800 = Color(0xFF1E293B);
  static const Color neutral900 = Color(0xFF0F172A);
  static const Color neutral950 = Color(0xFF020617);

  // ============================================
  // SEMANTIC COLORS
  // ============================================

  /// Success — Emerald green
  static const Color success = Color(0xFF10B981);
  static const Color successLight = Color(0xFFD1FAE5);
  static const Color successDark = Color(0xFF059669);

  /// Warning — Amber
  static const Color warning = Color(0xFFF59E0B);
  static const Color warningLight = Color(0xFFFEF3C7);
  static const Color warningDark = Color(0xFFD97706);

  /// Error — Rose red
  static const Color error = Color(0xFFF43F5E);
  static const Color errorLight = Color(0xFFFFE4E6);
  static const Color errorDark = Color(0xFFE11D48);

  /// Info — Sky blue
  static const Color info = Color(0xFF0EA5E9);
  static const Color infoLight = Color(0xFFE0F2FE);
  static const Color infoDark = Color(0xFF0284C7);

  // ============================================
  // GLASSMORPHISM COLORS
  // ============================================

  // Light Mode Glass
  static const Color glassWhite = Color(0x80FFFFFF); // 50% White
  static const Color glassWhiteStrong = Color(0xBFFFFFFF); // 75% White
  static const Color glassWhiteSubtle = Color(0x40FFFFFF); // 25% White
  static const Color glassBorderLight = Color(0x40FFFFFF); // 25% White border

  // Dark Mode Glass
  static const Color glassBlack = Color(0xB30F172A); // 70% dark slate
  static const Color glassBlackStrong = Color(0xE60F172A); // 90% dark slate
  static const Color glassBlackSubtle = Color(0x660F172A); // 40% dark slate
  static const Color glassBorderDark = Color(0x1AFFFFFF); // 10% White border

  // ============================================
  // SURFACE COLORS — LIGHT THEME
  // ============================================

  static const Color backgroundLight = Color(0xFFF8FAFC);
  static const Color surfaceLight = Color(0xFFFFFFFF);
  static const Color surfaceVariantLight = Color(0xFFF1F5F9);
  static const Color surfaceContainerLight = Color(0xFFF1F5F9);
  static const Color surfaceContainerHighLight = Color(0xFFE2E8F0);
  static const Color onBackgroundLight = Color(0xFF0F172A);
  static const Color onSurfaceLight = Color(0xFF0F172A);
  static const Color onSurfaceVariantLight = Color(0xFF475569);

  // ============================================
  // SURFACE COLORS — DARK THEME
  // ============================================

  static const Color backgroundDark = Color(0xFF020617);
  static const Color surfaceDark = Color(0xFF0F172A);
  static const Color surfaceVariantDark = Color(0xFF1E293B);
  static const Color surfaceContainerDark = Color(0xFF1E293B);
  static const Color surfaceContainerHighDark = Color(0xFF334155);
  static const Color onBackgroundDark = Color(0xFFF8FAFC);
  static const Color onSurfaceDark = Color(0xFFF1F5F9);
  static const Color onSurfaceVariantDark = Color(0xFF94A3B8);

  // ============================================
  // REAL ESTATE SPECIFIC COLORS
  // ============================================

  /// Price display
  static const Color priceTag = Color(0xFF059669);
  static const Color priceTagDark = Color(0xFF34D399);

  /// New listing badge
  static const Color newBadge = Color(0xFFEC4899);

  /// Sold/Under offer badge
  static const Color soldBadge = Color(0xFF64748B);

  // ============================================
  // SCORE RING COLORS
  // ============================================

  static const Color scoreExcellent = Color(0xFF10B981);
  static const Color scoreGood = Color(0xFF3B82F6);
  static const Color scoreAverage = Color(0xFFF59E0B);
  static const Color scorePoor = Color(0xFFF43F5E);

  // ============================================
  // GRADIENTS
  // ============================================

  static const LinearGradient primaryGradient = LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    colors: [Color(0xFF818CF8), Color(0xFF6366F1), Color(0xFF4F46E5)],
    stops: [0.0, 0.5, 1.0],
  );

  static const LinearGradient primarySoftGradient = LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    colors: [Color(0x266366F1), Color(0x1A4F46E5)],
  );

  static const LinearGradient accentGradient = LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    colors: [Color(0xFFFB923C), Color(0xFFF97316)],
  );

  static const LinearGradient darkBackgroundGradient = LinearGradient(
    begin: Alignment.topCenter,
    end: Alignment.bottomCenter,
    colors: [Color(0xFF0F172A), Color(0xFF020617)],
  );

  /// Subtle shimmer gradient for premium surfaces
  static const LinearGradient shimmerGradient = LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    colors: [Color(0x00FFFFFF), Color(0x0DFFFFFF), Color(0x00FFFFFF)],
    stops: [0.0, 0.5, 1.0],
  );

  /// Mesh-style gradient for hero sections
  static const LinearGradient heroGradient = LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    colors: [Color(0xFF6366F1), Color(0xFF8B5CF6), Color(0xFFA78BFA)],
    stops: [0.0, 0.5, 1.0],
  );
}
