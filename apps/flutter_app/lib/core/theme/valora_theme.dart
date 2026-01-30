import 'package:flutter/material.dart';
import 'valora_colors.dart';
import 'valora_typography.dart';
import 'valora_spacing.dart';

/// Valora Design System - Theme Configuration
///
/// Complete ThemeData configurations for light and dark modes.
/// Combines colors, typography, and spacing into cohesive themes.
abstract final class ValoraTheme {
  // ============================================
  // LIGHT THEME
  // ============================================

  static ThemeData get light => ThemeData(
        useMaterial3: true,
        brightness: Brightness.light,
        colorScheme: _lightColorScheme,
        textTheme: _textTheme,
        appBarTheme: _lightAppBarTheme,
        cardTheme: _lightCardTheme,
        elevatedButtonTheme: _elevatedButtonTheme,
        outlinedButtonTheme: _outlinedButtonTheme,
        textButtonTheme: _textButtonTheme,
        floatingActionButtonTheme: _lightFabTheme,
        inputDecorationTheme: _lightInputTheme,
        iconTheme: _lightIconTheme,
        dividerTheme: _lightDividerTheme,
        scaffoldBackgroundColor: ValoraColors.backgroundLight,
      );

  // ============================================
  // DARK THEME
  // ============================================

  static ThemeData get dark => ThemeData(
        useMaterial3: true,
        brightness: Brightness.dark,
        colorScheme: _darkColorScheme,
        textTheme: _textTheme,
        appBarTheme: _darkAppBarTheme,
        cardTheme: _darkCardTheme,
        elevatedButtonTheme: _elevatedButtonTheme,
        outlinedButtonTheme: _outlinedButtonTheme,
        textButtonTheme: _textButtonTheme,
        floatingActionButtonTheme: _darkFabTheme,
        inputDecorationTheme: _darkInputTheme,
        iconTheme: _darkIconTheme,
        dividerTheme: _darkDividerTheme,
        scaffoldBackgroundColor: ValoraColors.backgroundDark,
      );

  // ============================================
  // COLOR SCHEMES
  // ============================================

  static const ColorScheme _lightColorScheme = ColorScheme.light(
    primary: ValoraColors.primary,
    onPrimary: Colors.white,
    primaryContainer: ValoraColors.primaryLight,
    onPrimaryContainer: ValoraColors.primaryDark,
    secondary: ValoraColors.accent,
    onSecondary: Colors.white,
    secondaryContainer: ValoraColors.accentLight,
    onSecondaryContainer: ValoraColors.accentDark,
    tertiary: ValoraColors.info,
    error: ValoraColors.error,
    onError: Colors.white,
    errorContainer: ValoraColors.errorLight,
    onErrorContainer: ValoraColors.errorDark,
    surface: ValoraColors.surfaceLight,
    onSurface: ValoraColors.onSurfaceLight,
    surfaceContainerHighest: ValoraColors.surfaceVariantLight,
    onSurfaceVariant: ValoraColors.onSurfaceVariantLight,
    outline: ValoraColors.neutral300,
    outlineVariant: ValoraColors.neutral200,
    shadow: Colors.black,
    scrim: Colors.black,
  );

  static const ColorScheme _darkColorScheme = ColorScheme.dark(
    primary: ValoraColors.primaryLight,
    onPrimary: ValoraColors.primaryDark,
    primaryContainer: ValoraColors.primaryDark,
    onPrimaryContainer: ValoraColors.primaryLight,
    secondary: ValoraColors.accentLight,
    onSecondary: ValoraColors.accentDark,
    secondaryContainer: ValoraColors.accentDark,
    onSecondaryContainer: ValoraColors.accentLight,
    tertiary: ValoraColors.infoLight,
    error: ValoraColors.errorLight,
    onError: ValoraColors.errorDark,
    errorContainer: ValoraColors.errorDark,
    onErrorContainer: ValoraColors.errorLight,
    surface: ValoraColors.surfaceDark,
    onSurface: ValoraColors.onSurfaceDark,
    surfaceContainerHighest: ValoraColors.surfaceVariantDark,
    onSurfaceVariant: ValoraColors.onSurfaceVariantDark,
    outline: ValoraColors.neutral600,
    outlineVariant: ValoraColors.neutral700,
    shadow: Colors.black,
    scrim: Colors.black,
  );

  // ============================================
  // TEXT THEME
  // ============================================

  static const TextTheme _textTheme = TextTheme(
    displayLarge: ValoraTypography.displayLarge,
    displayMedium: ValoraTypography.displayMedium,
    displaySmall: ValoraTypography.displaySmall,
    headlineLarge: ValoraTypography.headlineLarge,
    headlineMedium: ValoraTypography.headlineMedium,
    headlineSmall: ValoraTypography.headlineSmall,
    titleLarge: ValoraTypography.titleLarge,
    titleMedium: ValoraTypography.titleMedium,
    titleSmall: ValoraTypography.titleSmall,
    bodyLarge: ValoraTypography.bodyLarge,
    bodyMedium: ValoraTypography.bodyMedium,
    bodySmall: ValoraTypography.bodySmall,
    labelLarge: ValoraTypography.labelLarge,
    labelMedium: ValoraTypography.labelMedium,
    labelSmall: ValoraTypography.labelSmall,
  );

  // ============================================
  // APP BAR THEME
  // ============================================

  static const AppBarTheme _lightAppBarTheme = AppBarTheme(
    elevation: 0,
    scrolledUnderElevation: 1,
    centerTitle: false,
    backgroundColor: ValoraColors.surfaceLight,
    foregroundColor: ValoraColors.onSurfaceLight,
    surfaceTintColor: Colors.transparent,
    titleTextStyle: ValoraTypography.titleLarge,
    iconTheme: IconThemeData(color: ValoraColors.onSurfaceLight),
  );

  static const AppBarTheme _darkAppBarTheme = AppBarTheme(
    elevation: 0,
    scrolledUnderElevation: 1,
    centerTitle: false,
    backgroundColor: ValoraColors.surfaceDark,
    foregroundColor: ValoraColors.onSurfaceDark,
    surfaceTintColor: Colors.transparent,
    titleTextStyle: ValoraTypography.titleLarge,
    iconTheme: IconThemeData(color: ValoraColors.onSurfaceDark),
  );

  // ============================================
  // CARD THEME
  // ============================================

  static CardThemeData get _lightCardTheme => CardThemeData(
        elevation: ValoraSpacing.elevationSm,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
        ),
        color: ValoraColors.surfaceLight,
        surfaceTintColor: Colors.transparent,
        margin: EdgeInsets.zero,
        clipBehavior: Clip.antiAlias,
      );

  static CardThemeData get _darkCardTheme => CardThemeData(
        elevation: ValoraSpacing.elevationSm,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
        ),
        color: ValoraColors.surfaceDark,
        surfaceTintColor: Colors.transparent,
        margin: EdgeInsets.zero,
        clipBehavior: Clip.antiAlias,
      );

  // ============================================
  // BUTTON THEMES
  // ============================================

  static ElevatedButtonThemeData get _elevatedButtonTheme =>
      ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          elevation: ValoraSpacing.elevationSm,
          padding: const EdgeInsets.symmetric(
            horizontal: ValoraSpacing.lg,
            vertical: ValoraSpacing.md,
          ),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
          ),
          textStyle: ValoraTypography.labelLarge,
          minimumSize: const Size(0, ValoraSpacing.buttonHeightMd),
        ),
      );

  static OutlinedButtonThemeData get _outlinedButtonTheme =>
      OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          padding: const EdgeInsets.symmetric(
            horizontal: ValoraSpacing.lg,
            vertical: ValoraSpacing.md,
          ),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
          ),
          textStyle: ValoraTypography.labelLarge,
          minimumSize: const Size(0, ValoraSpacing.buttonHeightMd),
        ),
      );

  static TextButtonThemeData get _textButtonTheme => TextButtonThemeData(
        style: TextButton.styleFrom(
          padding: const EdgeInsets.symmetric(
            horizontal: ValoraSpacing.md,
            vertical: ValoraSpacing.sm,
          ),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
          ),
          textStyle: ValoraTypography.labelLarge,
        ),
      );

  // ============================================
  // FAB THEME
  // ============================================

  static FloatingActionButtonThemeData get _lightFabTheme =>
      FloatingActionButtonThemeData(
        elevation: ValoraSpacing.elevationLg,
        backgroundColor: ValoraColors.primary,
        foregroundColor: Colors.white,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
        ),
      );

  static FloatingActionButtonThemeData get _darkFabTheme =>
      FloatingActionButtonThemeData(
        elevation: ValoraSpacing.elevationLg,
        backgroundColor: ValoraColors.primaryLight,
        foregroundColor: ValoraColors.primaryDark,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
        ),
      );

  // ============================================
  // INPUT THEME
  // ============================================

  static InputDecorationTheme get _lightInputTheme => InputDecorationTheme(
        filled: true,
        fillColor: ValoraColors.surfaceVariantLight,
        contentPadding: const EdgeInsets.symmetric(
          horizontal: ValoraSpacing.md,
          vertical: ValoraSpacing.md,
        ),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
          borderSide: BorderSide.none,
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
          borderSide: BorderSide.none,
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
          borderSide: const BorderSide(color: ValoraColors.primary, width: 2),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
          borderSide: const BorderSide(color: ValoraColors.error, width: 1),
        ),
        focusedErrorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
          borderSide: const BorderSide(color: ValoraColors.error, width: 2),
        ),
        hintStyle: ValoraTypography.bodyMedium.copyWith(
          color: ValoraColors.onSurfaceVariantLight,
        ),
        labelStyle: ValoraTypography.bodyMedium,
        errorStyle: ValoraTypography.bodySmall.copyWith(
          color: ValoraColors.error,
        ),
      );

  static InputDecorationTheme get _darkInputTheme => InputDecorationTheme(
        filled: true,
        fillColor: ValoraColors.surfaceVariantDark,
        contentPadding: const EdgeInsets.symmetric(
          horizontal: ValoraSpacing.md,
          vertical: ValoraSpacing.md,
        ),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
          borderSide: BorderSide.none,
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
          borderSide: BorderSide.none,
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
          borderSide:
              const BorderSide(color: ValoraColors.primaryLight, width: 2),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
          borderSide:
              const BorderSide(color: ValoraColors.errorLight, width: 1),
        ),
        focusedErrorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
          borderSide:
              const BorderSide(color: ValoraColors.errorLight, width: 2),
        ),
        hintStyle: ValoraTypography.bodyMedium.copyWith(
          color: ValoraColors.onSurfaceVariantDark,
        ),
        labelStyle: ValoraTypography.bodyMedium,
        errorStyle: ValoraTypography.bodySmall.copyWith(
          color: ValoraColors.errorLight,
        ),
      );

  // ============================================
  // ICON THEME
  // ============================================

  static const IconThemeData _lightIconTheme = IconThemeData(
    color: ValoraColors.onSurfaceLight,
    size: ValoraSpacing.iconSizeMd,
  );

  static const IconThemeData _darkIconTheme = IconThemeData(
    color: ValoraColors.onSurfaceDark,
    size: ValoraSpacing.iconSizeMd,
  );

  // ============================================
  // DIVIDER THEME
  // ============================================

  static const DividerThemeData _lightDividerTheme = DividerThemeData(
    color: ValoraColors.neutral200,
    thickness: 1,
    space: ValoraSpacing.md,
  );

  static const DividerThemeData _darkDividerTheme = DividerThemeData(
    color: ValoraColors.neutral700,
    thickness: 1,
    space: ValoraSpacing.md,
  );
}
