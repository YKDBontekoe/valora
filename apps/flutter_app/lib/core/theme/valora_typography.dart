import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

/// Valora Design System - Typography (v2)
///
/// A refined type scale using Google Fonts for consistent rendering.
/// Uses Inter for body/UI text and a display-optimized weight stack.
abstract final class ValoraTypography {
  // ============================================
  // FONT FAMILIES
  // ============================================

  /// Primary font family via Google Fonts — Inter (clean, geometric, excellent for UI)
  static String get fontFamily => GoogleFonts.inter().fontFamily!;

  /// Display/heading override — Plus Jakarta Sans (modern, geometric display face)
  static String get displayFontFamily => GoogleFonts.plusJakartaSans().fontFamily!;

  // ============================================
  // FONT WEIGHTS
  // ============================================

  static const FontWeight light = FontWeight.w300;
  static const FontWeight regular = FontWeight.w400;
  static const FontWeight medium = FontWeight.w500;
  static const FontWeight semiBold = FontWeight.w600;
  static const FontWeight bold = FontWeight.w700;
  static const FontWeight extraBold = FontWeight.w800;

  // ============================================
  // TEXT STYLES - DISPLAY (Plus Jakarta Sans)
  // ============================================

  /// Display Large - Hero headlines, onboarding
  static TextStyle get displayLarge => GoogleFonts.plusJakartaSans(
    fontSize: 56,
    fontWeight: extraBold,
    letterSpacing: -1.5,
    height: 1.1,
  );

  /// Display Medium - Section heroes
  static TextStyle get displayMedium => GoogleFonts.plusJakartaSans(
    fontSize: 44,
    fontWeight: bold,
    letterSpacing: -0.8,
    height: 1.14,
  );

  /// Display Small - Sub-heroes
  static TextStyle get displaySmall => GoogleFonts.plusJakartaSans(
    fontSize: 36,
    fontWeight: bold,
    letterSpacing: -0.5,
    height: 1.2,
  );

  // ============================================
  // TEXT STYLES - HEADLINE (Plus Jakarta Sans)
  // ============================================

  /// Headline Large - Page titles
  static TextStyle get headlineLarge => GoogleFonts.plusJakartaSans(
    fontSize: 32,
    fontWeight: bold,
    letterSpacing: -0.5,
    height: 1.2,
  );

  /// Headline Medium - Card titles
  static TextStyle get headlineMedium => GoogleFonts.plusJakartaSans(
    fontSize: 28,
    fontWeight: semiBold,
    letterSpacing: -0.3,
    height: 1.25,
  );

  /// Headline Small - Component titles
  static TextStyle get headlineSmall => GoogleFonts.plusJakartaSans(
    fontSize: 24,
    fontWeight: semiBold,
    letterSpacing: -0.2,
    height: 1.3,
  );

  // ============================================
  // TEXT STYLES - TITLE (Inter)
  // ============================================

  /// Title Large - Prominent labels
  static TextStyle get titleLarge => GoogleFonts.inter(
    fontSize: 22,
    fontWeight: semiBold,
    letterSpacing: -0.2,
    height: 1.27,
  );

  /// Title Medium - Secondary labels
  static TextStyle get titleMedium => GoogleFonts.inter(
    fontSize: 16,
    fontWeight: semiBold,
    letterSpacing: 0,
    height: 1.5,
  );

  /// Title Small - Tertiary labels
  static TextStyle get titleSmall => GoogleFonts.inter(
    fontSize: 14,
    fontWeight: semiBold,
    letterSpacing: 0,
    height: 1.43,
  );

  // ============================================
  // TEXT STYLES - BODY (Inter)
  // ============================================

  /// Body Large - Primary body text
  static TextStyle get bodyLarge => GoogleFonts.inter(
    fontSize: 16,
    fontWeight: regular,
    letterSpacing: 0.1,
    height: 1.6,
  );

  /// Body Medium - Secondary body text
  static TextStyle get bodyMedium => GoogleFonts.inter(
    fontSize: 14,
    fontWeight: regular,
    letterSpacing: 0.1,
    height: 1.5,
  );

  /// Body Small - Tertiary body text
  static TextStyle get bodySmall => GoogleFonts.inter(
    fontSize: 12,
    fontWeight: regular,
    letterSpacing: 0.2,
    height: 1.4,
  );

  // ============================================
  // TEXT STYLES - LABEL (Inter)
  // ============================================

  /// Label Large - Prominent labels, buttons
  static TextStyle get labelLarge => GoogleFonts.inter(
    fontSize: 14,
    fontWeight: medium,
    letterSpacing: 0.1,
    height: 1.43,
  );

  /// Label Medium - Secondary labels
  static TextStyle get labelMedium => GoogleFonts.inter(
    fontSize: 12,
    fontWeight: medium,
    letterSpacing: 0.3,
    height: 1.33,
  );

  /// Label Small - Captions, metadata
  static TextStyle get labelSmall => GoogleFonts.inter(
    fontSize: 11,
    fontWeight: medium,
    letterSpacing: 0.3,
    height: 1.45,
  );

  // ============================================
  // SPECIAL STYLES
  // ============================================

  /// Price display - Large, bold, tight tracking
  static TextStyle get priceDisplay => GoogleFonts.plusJakartaSans(
    fontSize: 26,
    fontWeight: extraBold,
    letterSpacing: -0.5,
    height: 1.15,
  );

  /// Price display small
  static TextStyle get priceDisplaySmall => GoogleFonts.plusJakartaSans(
    fontSize: 18,
    fontWeight: bold,
    letterSpacing: -0.3,
    height: 1.2,
  );

  /// Address display - Medium weight for addresses
  static TextStyle get addressDisplay => GoogleFonts.inter(
    fontSize: 17,
    fontWeight: semiBold,
    letterSpacing: -0.1,
    height: 1.35,
  );

  /// Metadata - Small, muted for specs
  static TextStyle get metadata => GoogleFonts.inter(
    fontSize: 13,
    fontWeight: regular,
    letterSpacing: 0.1,
    height: 1.38,
  );

  /// Score display - Monospaced feel for numbers
  static TextStyle get scoreDisplay => GoogleFonts.plusJakartaSans(
    fontSize: 20,
    fontWeight: extraBold,
    letterSpacing: -0.3,
    height: 1.0,
  );

  /// Overline - small uppercase text for section headers
  static TextStyle get overline => GoogleFonts.inter(
    fontSize: 11,
    fontWeight: semiBold,
    letterSpacing: 1.2,
    height: 1.45,
  );

  // ============================================
  // TEXT THEME (for ThemeData)
  // ============================================

  static TextTheme get textTheme => TextTheme(
    displayLarge: displayLarge,
    displayMedium: displayMedium,
    displaySmall: displaySmall,
    headlineLarge: headlineLarge,
    headlineMedium: headlineMedium,
    headlineSmall: headlineSmall,
    titleLarge: titleLarge,
    titleMedium: titleMedium,
    titleSmall: titleSmall,
    bodyLarge: bodyLarge,
    bodyMedium: bodyMedium,
    bodySmall: bodySmall,
    labelLarge: labelLarge,
    labelMedium: labelMedium,
    labelSmall: labelSmall,
  );
}
