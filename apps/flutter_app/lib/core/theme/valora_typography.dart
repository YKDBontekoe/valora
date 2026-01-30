import 'package:flutter/material.dart';

/// Valora Design System - Typography
///
/// A type scale based on Material Design 3 with Inter font family.
/// Follows a modular scale for consistent visual hierarchy.
abstract final class ValoraTypography {
  // ============================================
  // FONT FAMILY
  // ============================================

  /// Primary font family - Inter
  /// Falls back to system fonts for broad compatibility
  static const String fontFamily = 'Inter';

  static const List<String> fontFamilyFallback = [
    '-apple-system',
    'BlinkMacSystemFont',
    'Segoe UI',
    'Roboto',
    'Helvetica Neue',
    'Arial',
    'sans-serif',
  ];

  // ============================================
  // FONT WEIGHTS
  // ============================================

  static const FontWeight light = FontWeight.w300;
  static const FontWeight regular = FontWeight.w400;
  static const FontWeight medium = FontWeight.w500;
  static const FontWeight semiBold = FontWeight.w600;
  static const FontWeight bold = FontWeight.w700;

  // ============================================
  // TEXT STYLES - DISPLAY
  // ============================================

  /// Display Large - Hero headlines
  static const TextStyle displayLarge = TextStyle(
    fontFamily: fontFamily,
    fontFamilyFallback: fontFamilyFallback,
    fontSize: 57,
    fontWeight: bold,
    letterSpacing: -0.25,
    height: 1.12,
  );

  /// Display Medium - Section headers
  static const TextStyle displayMedium = TextStyle(
    fontFamily: fontFamily,
    fontFamilyFallback: fontFamilyFallback,
    fontSize: 45,
    fontWeight: bold,
    letterSpacing: 0,
    height: 1.16,
  );

  /// Display Small - Sub-section headers
  static const TextStyle displaySmall = TextStyle(
    fontFamily: fontFamily,
    fontFamilyFallback: fontFamilyFallback,
    fontSize: 36,
    fontWeight: semiBold,
    letterSpacing: 0,
    height: 1.22,
  );

  // ============================================
  // TEXT STYLES - HEADLINE
  // ============================================

  /// Headline Large - Page titles
  static const TextStyle headlineLarge = TextStyle(
    fontFamily: fontFamily,
    fontFamilyFallback: fontFamilyFallback,
    fontSize: 32,
    fontWeight: semiBold,
    letterSpacing: 0,
    height: 1.25,
  );

  /// Headline Medium - Card titles
  static const TextStyle headlineMedium = TextStyle(
    fontFamily: fontFamily,
    fontFamilyFallback: fontFamilyFallback,
    fontSize: 28,
    fontWeight: semiBold,
    letterSpacing: 0,
    height: 1.29,
  );

  /// Headline Small - Component titles
  static const TextStyle headlineSmall = TextStyle(
    fontFamily: fontFamily,
    fontFamilyFallback: fontFamilyFallback,
    fontSize: 24,
    fontWeight: medium,
    letterSpacing: 0,
    height: 1.33,
  );

  // ============================================
  // TEXT STYLES - TITLE
  // ============================================

  /// Title Large - Prominent labels
  static const TextStyle titleLarge = TextStyle(
    fontFamily: fontFamily,
    fontFamilyFallback: fontFamilyFallback,
    fontSize: 22,
    fontWeight: medium,
    letterSpacing: 0,
    height: 1.27,
  );

  /// Title Medium - Secondary labels
  static const TextStyle titleMedium = TextStyle(
    fontFamily: fontFamily,
    fontFamilyFallback: fontFamilyFallback,
    fontSize: 16,
    fontWeight: semiBold,
    letterSpacing: 0.15,
    height: 1.5,
  );

  /// Title Small - Tertiary labels
  static const TextStyle titleSmall = TextStyle(
    fontFamily: fontFamily,
    fontFamilyFallback: fontFamilyFallback,
    fontSize: 14,
    fontWeight: semiBold,
    letterSpacing: 0.1,
    height: 1.43,
  );

  // ============================================
  // TEXT STYLES - BODY
  // ============================================

  /// Body Large - Primary body text
  static const TextStyle bodyLarge = TextStyle(
    fontFamily: fontFamily,
    fontFamilyFallback: fontFamilyFallback,
    fontSize: 16,
    fontWeight: regular,
    letterSpacing: 0.5,
    height: 1.5,
  );

  /// Body Medium - Secondary body text
  static const TextStyle bodyMedium = TextStyle(
    fontFamily: fontFamily,
    fontFamilyFallback: fontFamilyFallback,
    fontSize: 14,
    fontWeight: regular,
    letterSpacing: 0.25,
    height: 1.43,
  );

  /// Body Small - Tertiary body text
  static const TextStyle bodySmall = TextStyle(
    fontFamily: fontFamily,
    fontFamilyFallback: fontFamilyFallback,
    fontSize: 12,
    fontWeight: regular,
    letterSpacing: 0.4,
    height: 1.33,
  );

  // ============================================
  // TEXT STYLES - LABEL
  // ============================================

  /// Label Large - Prominent labels, buttons
  static const TextStyle labelLarge = TextStyle(
    fontFamily: fontFamily,
    fontFamilyFallback: fontFamilyFallback,
    fontSize: 14,
    fontWeight: medium,
    letterSpacing: 0.1,
    height: 1.43,
  );

  /// Label Medium - Secondary labels
  static const TextStyle labelMedium = TextStyle(
    fontFamily: fontFamily,
    fontFamilyFallback: fontFamilyFallback,
    fontSize: 12,
    fontWeight: medium,
    letterSpacing: 0.5,
    height: 1.33,
  );

  /// Label Small - Captions, metadata
  static const TextStyle labelSmall = TextStyle(
    fontFamily: fontFamily,
    fontFamilyFallback: fontFamilyFallback,
    fontSize: 11,
    fontWeight: medium,
    letterSpacing: 0.5,
    height: 1.45,
  );

  // ============================================
  // SPECIAL STYLES
  // ============================================

  /// Price display - Large, bold for property prices
  static const TextStyle priceDisplay = TextStyle(
    fontFamily: fontFamily,
    fontFamilyFallback: fontFamilyFallback,
    fontSize: 24,
    fontWeight: bold,
    letterSpacing: -0.5,
    height: 1.2,
  );

  /// Address display - Medium weight for addresses
  static const TextStyle addressDisplay = TextStyle(
    fontFamily: fontFamily,
    fontFamilyFallback: fontFamilyFallback,
    fontSize: 18,
    fontWeight: semiBold,
    letterSpacing: 0,
    height: 1.33,
  );

  /// Metadata - Small, muted for specs
  static const TextStyle metadata = TextStyle(
    fontFamily: fontFamily,
    fontFamilyFallback: fontFamilyFallback,
    fontSize: 13,
    fontWeight: regular,
    letterSpacing: 0.2,
    height: 1.38,
  );
}
