import 'package:flutter/material.dart';
import 'valora_colors.dart';
import 'valora_typography.dart';
import 'valora_spacing.dart';

/// Valora Design System - Theme Configuration (v2)
///
/// Premium ThemeData configurations with refined surfaces,
/// sophisticated component themes, and smooth transitions.
abstract final class ValoraTheme {
  // ============================================
  // LIGHT THEME
  // ============================================

  static ThemeData get light => ThemeData(
    useMaterial3: true,
    brightness: Brightness.light,
    colorScheme: _lightColorScheme,
    textTheme: ValoraTypography.textTheme,
    appBarTheme: _lightAppBarTheme,
    cardTheme: _lightCardTheme,
    elevatedButtonTheme: _elevatedButtonTheme,
    outlinedButtonTheme: _outlinedButtonTheme,
    textButtonTheme: _textButtonTheme,
    floatingActionButtonTheme: _lightFabTheme,
    inputDecorationTheme: _lightInputTheme,
    iconTheme: _lightIconTheme,
    dividerTheme: _lightDividerTheme,
    chipTheme: _lightChipTheme,
    dialogTheme: _lightDialogTheme,
    bottomSheetTheme: _lightBottomSheetTheme,
    navigationBarTheme: _lightNavigationBarTheme,
    snackBarTheme: _lightSnackBarTheme,
    tooltipTheme: _lightTooltipTheme,
    progressIndicatorTheme: _progressTheme,
    scaffoldBackgroundColor: ValoraColors.backgroundLight,
    splashColor: ValoraColors.primary.withValues(alpha: 0.08),
    highlightColor: ValoraColors.primary.withValues(alpha: 0.04),
    pageTransitionsTheme: const PageTransitionsTheme(
      builders: {
        TargetPlatform.android: ZoomPageTransitionsBuilder(),
        TargetPlatform.iOS: CupertinoPageTransitionsBuilder(),
        TargetPlatform.macOS: CupertinoPageTransitionsBuilder(),
      },
    ),
  );

  // ============================================
  // DARK THEME
  // ============================================

  static ThemeData get dark => ThemeData(
    useMaterial3: true,
    brightness: Brightness.dark,
    colorScheme: _darkColorScheme,
    textTheme: ValoraTypography.textTheme,
    appBarTheme: _darkAppBarTheme,
    cardTheme: _darkCardTheme,
    elevatedButtonTheme: _elevatedButtonTheme,
    outlinedButtonTheme: _outlinedButtonTheme,
    textButtonTheme: _textButtonTheme,
    floatingActionButtonTheme: _darkFabTheme,
    inputDecorationTheme: _darkInputTheme,
    iconTheme: _darkIconTheme,
    dividerTheme: _darkDividerTheme,
    chipTheme: _darkChipTheme,
    dialogTheme: _darkDialogTheme,
    bottomSheetTheme: _darkBottomSheetTheme,
    navigationBarTheme: _darkNavigationBarTheme,
    snackBarTheme: _darkSnackBarTheme,
    tooltipTheme: _darkTooltipTheme,
    progressIndicatorTheme: _progressTheme,
    scaffoldBackgroundColor: ValoraColors.backgroundDark,
    splashColor: ValoraColors.primaryLight.withValues(alpha: 0.08),
    highlightColor: ValoraColors.primaryLight.withValues(alpha: 0.04),
    pageTransitionsTheme: const PageTransitionsTheme(
      builders: {
        TargetPlatform.android: ZoomPageTransitionsBuilder(),
        TargetPlatform.iOS: CupertinoPageTransitionsBuilder(),
        TargetPlatform.macOS: CupertinoPageTransitionsBuilder(),
      },
    ),
  );

  // ============================================
  // COLOR SCHEMES
  // ============================================

  static const ColorScheme _lightColorScheme = ColorScheme.light(
    primary: ValoraColors.primary,
    onPrimary: Colors.white,
    primaryContainer: ValoraColors.primaryLighter,
    onPrimaryContainer: ValoraColors.primaryDarker,
    secondary: ValoraColors.accent,
    onSecondary: Colors.white,
    secondaryContainer: ValoraColors.accentLighter,
    onSecondaryContainer: ValoraColors.accentDarker,
    tertiary: ValoraColors.info,
    error: ValoraColors.error,
    onError: Colors.white,
    errorContainer: ValoraColors.errorLight,
    onErrorContainer: ValoraColors.errorDark,
    surface: ValoraColors.surfaceLight,
    onSurface: ValoraColors.onSurfaceLight,
    surfaceContainerHighest: ValoraColors.surfaceContainerHighLight,
    onSurfaceVariant: ValoraColors.onSurfaceVariantLight,
    outline: ValoraColors.neutral300,
    outlineVariant: ValoraColors.neutral200,
    shadow: Colors.black12,
    scrim: Colors.black26,
  );

  static const ColorScheme _darkColorScheme = ColorScheme.dark(
    primary: ValoraColors.primaryLight,
    onPrimary: ValoraColors.primaryDarker,
    primaryContainer: ValoraColors.primaryDarker,
    onPrimaryContainer: ValoraColors.primaryLighter,
    secondary: ValoraColors.accentLight,
    onSecondary: ValoraColors.accentDarker,
    secondaryContainer: ValoraColors.accentDarker,
    onSecondaryContainer: ValoraColors.accentLighter,
    tertiary: ValoraColors.infoLight,
    error: ValoraColors.errorLight,
    onError: ValoraColors.errorDark,
    errorContainer: ValoraColors.errorDark,
    onErrorContainer: ValoraColors.errorLight,
    surface: ValoraColors.surfaceDark,
    onSurface: ValoraColors.onSurfaceDark,
    surfaceContainerHighest: ValoraColors.surfaceContainerHighDark,
    onSurfaceVariant: ValoraColors.onSurfaceVariantDark,
    outline: ValoraColors.neutral600,
    outlineVariant: ValoraColors.neutral700,
    shadow: Colors.black54,
    scrim: Colors.black,
  );

  // ============================================
  // APP BAR THEME
  // ============================================

  static AppBarTheme get _lightAppBarTheme => AppBarTheme(
    elevation: 0,
    scrolledUnderElevation: 0,
    centerTitle: false,
    backgroundColor: Colors.transparent,
    foregroundColor: ValoraColors.onSurfaceLight,
    surfaceTintColor: Colors.transparent,
    titleTextStyle: ValoraTypography.titleLarge.copyWith(
      color: ValoraColors.onSurfaceLight,
    ),
    iconTheme: const IconThemeData(color: ValoraColors.onSurfaceLight),
  );

  static AppBarTheme get _darkAppBarTheme => AppBarTheme(
    elevation: 0,
    scrolledUnderElevation: 0,
    centerTitle: false,
    backgroundColor: Colors.transparent,
    foregroundColor: ValoraColors.onSurfaceDark,
    surfaceTintColor: Colors.transparent,
    titleTextStyle: ValoraTypography.titleLarge.copyWith(
      color: ValoraColors.onSurfaceDark,
    ),
    iconTheme: const IconThemeData(color: ValoraColors.onSurfaceDark),
  );

  // ============================================
  // CARD THEME
  // ============================================

  static CardThemeData get _lightCardTheme => CardThemeData(
    elevation: ValoraSpacing.elevationNone,
    shadowColor: Colors.black.withValues(alpha: 0.04),
    shape: RoundedRectangleBorder(
      borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
      side: BorderSide(
        color: ValoraColors.neutral200.withValues(alpha: 0.6),
        width: 1,
      ),
    ),
    color: ValoraColors.surfaceLight,
    surfaceTintColor: Colors.transparent,
    margin: EdgeInsets.zero,
    clipBehavior: Clip.antiAlias,
  );

  static CardThemeData get _darkCardTheme => CardThemeData(
    elevation: ValoraSpacing.elevationNone,
    shadowColor: Colors.black.withValues(alpha: 0.3),
    shape: RoundedRectangleBorder(
      borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
      side: BorderSide(
        color: ValoraColors.neutral700.withValues(alpha: 0.4),
        width: 1,
      ),
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
          elevation: 0,
          shadowColor: ValoraColors.primary.withValues(alpha: 0.3),
          padding: const EdgeInsets.symmetric(
            horizontal: ValoraSpacing.lg,
            vertical: ValoraSpacing.md,
          ),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(ValoraSpacing.radiusXl),
          ),
          textStyle: ValoraTypography.labelLarge.copyWith(
            fontWeight: FontWeight.w600,
          ),
          minimumSize: const Size(0, ValoraSpacing.buttonHeightMd),
          tapTargetSize: MaterialTapTargetSize.shrinkWrap,
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
            borderRadius: BorderRadius.circular(ValoraSpacing.radiusXl),
          ),
          side: BorderSide(
            color: ValoraColors.neutral300.withValues(alpha: 0.8),
            width: 1.5,
          ),
          textStyle: ValoraTypography.labelLarge.copyWith(
            fontWeight: FontWeight.w600,
          ),
          minimumSize: const Size(0, ValoraSpacing.buttonHeightMd),
          tapTargetSize: MaterialTapTargetSize.shrinkWrap,
        ),
      );

  static TextButtonThemeData get _textButtonTheme => TextButtonThemeData(
    style: TextButton.styleFrom(
      padding: const EdgeInsets.symmetric(
        horizontal: ValoraSpacing.md,
        vertical: ValoraSpacing.sm,
      ),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(ValoraSpacing.radiusXl),
      ),
      textStyle: ValoraTypography.labelLarge.copyWith(
        fontWeight: FontWeight.w600,
      ),
      tapTargetSize: MaterialTapTargetSize.shrinkWrap,
    ),
  );

  // ============================================
  // FAB THEME
  // ============================================

  static FloatingActionButtonThemeData get _lightFabTheme =>
      FloatingActionButtonThemeData(
        elevation: ValoraSpacing.elevationMd,
        backgroundColor: ValoraColors.primary,
        foregroundColor: Colors.white,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(ValoraSpacing.radiusXl),
        ),
      );

  static FloatingActionButtonThemeData get _darkFabTheme =>
      FloatingActionButtonThemeData(
        elevation: ValoraSpacing.elevationMd,
        backgroundColor: ValoraColors.primaryLight,
        foregroundColor: ValoraColors.primaryDarker,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(ValoraSpacing.radiusXl),
        ),
      );

  // ============================================
  // INPUT THEME
  // ============================================

  static InputDecorationTheme get _lightInputTheme => InputDecorationTheme(
    filled: true,
    fillColor: ValoraColors.neutral50.withValues(alpha: 0.5),
    contentPadding: const EdgeInsets.symmetric(
      horizontal: ValoraSpacing.lg,
      vertical: ValoraSpacing.md,
    ),
    border: OutlineInputBorder(
      borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
      borderSide: BorderSide.none,
    ),
    enabledBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
      borderSide: BorderSide(
        color: ValoraColors.neutral200.withValues(alpha: 0.5),
      ),
    ),
    focusedBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
      borderSide: const BorderSide(color: ValoraColors.primary, width: 2),
    ),
    errorBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
      borderSide: BorderSide(
        color: ValoraColors.error.withValues(alpha: 0.8),
        width: 1,
      ),
    ),
    focusedErrorBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
      borderSide: const BorderSide(color: ValoraColors.error, width: 2),
    ),
    hintStyle: ValoraTypography.bodyMedium.copyWith(
      color: ValoraColors.neutral400,
    ),
    labelStyle: ValoraTypography.bodyMedium,
  );

  static InputDecorationTheme get _darkInputTheme => InputDecorationTheme(
    filled: true,
    fillColor: ValoraColors.surfaceVariantDark.withValues(alpha: 0.3),
    contentPadding: const EdgeInsets.symmetric(
      horizontal: ValoraSpacing.lg,
      vertical: ValoraSpacing.md,
    ),
    border: OutlineInputBorder(
      borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
      borderSide: BorderSide.none,
    ),
    enabledBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
      borderSide: BorderSide(
        color: ValoraColors.neutral700.withValues(alpha: 0.5),
      ),
    ),
    focusedBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
      borderSide: const BorderSide(
        color: ValoraColors.primaryLight,
        width: 2,
      ),
    ),
    errorBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
      borderSide: BorderSide(
        color: ValoraColors.errorLight.withValues(alpha: 0.8),
        width: 1,
      ),
    ),
    focusedErrorBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
      borderSide: const BorderSide(color: ValoraColors.errorLight, width: 2),
    ),
    hintStyle: ValoraTypography.bodyMedium.copyWith(
      color: ValoraColors.neutral500,
    ),
    labelStyle: ValoraTypography.bodyMedium,
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
    color: ValoraColors.neutral800,
    thickness: 1,
    space: ValoraSpacing.md,
  );

  // ============================================
  // CHIP THEME
  // ============================================

  static ChipThemeData get _lightChipTheme => ChipThemeData(
    shape: RoundedRectangleBorder(
      borderRadius: BorderRadius.circular(ValoraSpacing.radiusFull),
    ),
    side: BorderSide(
      color: ValoraColors.neutral200.withValues(alpha: 0.8),
    ),
    labelStyle: ValoraTypography.labelMedium,
    secondaryLabelStyle: ValoraTypography.labelMedium,
    selectedColor: ValoraColors.primary.withValues(alpha: 0.12),
    disabledColor: ValoraColors.neutral100,
    backgroundColor: ValoraColors.surfaceLight,
    padding: const EdgeInsets.symmetric(
      horizontal: ValoraSpacing.sm,
      vertical: ValoraSpacing.xs,
    ),
  );

  static ChipThemeData get _darkChipTheme => ChipThemeData(
    shape: RoundedRectangleBorder(
      borderRadius: BorderRadius.circular(ValoraSpacing.radiusFull),
    ),
    side: BorderSide(
      color: ValoraColors.neutral700.withValues(alpha: 0.6),
    ),
    labelStyle: ValoraTypography.labelMedium,
    secondaryLabelStyle: ValoraTypography.labelMedium,
    selectedColor: ValoraColors.primary.withValues(alpha: 0.16),
    disabledColor: ValoraColors.neutral800,
    backgroundColor: ValoraColors.surfaceVariantDark,
    padding: const EdgeInsets.symmetric(
      horizontal: ValoraSpacing.sm,
      vertical: ValoraSpacing.xs,
    ),
  );

  // ============================================
  // DIALOG THEME
  // ============================================

  static DialogThemeData get _lightDialogTheme => DialogThemeData(
    backgroundColor: ValoraColors.surfaceLight,
    shape: RoundedRectangleBorder(
      borderRadius: BorderRadius.circular(ValoraSpacing.radiusXxl + 4),
    ),
    elevation: ValoraSpacing.elevationLg,
    shadowColor: Colors.black.withValues(alpha: 0.08),
  );

  static DialogThemeData get _darkDialogTheme => DialogThemeData(
    backgroundColor: ValoraColors.surfaceVariantDark,
    shape: RoundedRectangleBorder(
      borderRadius: BorderRadius.circular(ValoraSpacing.radiusXxl + 4),
    ),
    elevation: ValoraSpacing.elevationLg,
    shadowColor: Colors.black.withValues(alpha: 0.3),
  );

  // ============================================
  // BOTTOM SHEET THEME
  // ============================================

  static BottomSheetThemeData get _lightBottomSheetTheme =>
      BottomSheetThemeData(
        backgroundColor: ValoraColors.surfaceLight,
        surfaceTintColor: Colors.transparent,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.vertical(
            top: Radius.circular(ValoraSpacing.radiusXxl),
          ),
        ),
        showDragHandle: true,
        dragHandleColor: ValoraColors.neutral300,
        dragHandleSize: const Size(40, 4),
        shadowColor: Colors.black.withValues(alpha: 0.1),
        modalBarrierColor: Colors.black.withValues(alpha: 0.2),
      );

  static BottomSheetThemeData get _darkBottomSheetTheme =>
      BottomSheetThemeData(
        backgroundColor: ValoraColors.surfaceVariantDark,
        surfaceTintColor: Colors.transparent,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.vertical(
            top: Radius.circular(ValoraSpacing.radiusXxl),
          ),
        ),
        showDragHandle: true,
        dragHandleColor: ValoraColors.neutral600,
        dragHandleSize: const Size(40, 4),
        shadowColor: Colors.black.withValues(alpha: 0.5),
        modalBarrierColor: Colors.black.withValues(alpha: 0.5),
      );

  // ============================================
  // NAVIGATION BAR THEME
  // ============================================

  static NavigationBarThemeData get _lightNavigationBarTheme =>
      NavigationBarThemeData(
        indicatorColor: ValoraColors.primary.withValues(alpha: 0.12),
        backgroundColor: ValoraColors.surfaceLight.withValues(alpha: 0.9),
        height: ValoraSpacing.navBarHeight + ValoraSpacing.sm,
        elevation: 0,
        labelTextStyle: WidgetStateProperty.resolveWith<TextStyle>((states) {
          if (states.contains(WidgetState.selected)) {
            return ValoraTypography.labelMedium.copyWith(
              color: ValoraColors.primary,
              fontWeight: FontWeight.w700,
            );
          }
          return ValoraTypography.labelMedium.copyWith(
            color: ValoraColors.neutral500,
          );
        }),
      );

  static NavigationBarThemeData get _darkNavigationBarTheme =>
      NavigationBarThemeData(
        indicatorColor: ValoraColors.primaryLight.withValues(alpha: 0.15),
        backgroundColor: ValoraColors.surfaceDark.withValues(alpha: 0.9),
        height: ValoraSpacing.navBarHeight + ValoraSpacing.sm,
        elevation: 0,
        labelTextStyle: WidgetStateProperty.resolveWith<TextStyle>((states) {
          if (states.contains(WidgetState.selected)) {
            return ValoraTypography.labelMedium.copyWith(
              color: ValoraColors.primaryLight,
              fontWeight: FontWeight.w700,
            );
          }
          return ValoraTypography.labelMedium.copyWith(
            color: ValoraColors.neutral400,
          );
        }),
      );

  // ============================================
  // SNACK BAR THEME
  // ============================================

  static SnackBarThemeData get _lightSnackBarTheme => SnackBarThemeData(
    behavior: SnackBarBehavior.floating,
    backgroundColor: ValoraColors.neutral900,
    contentTextStyle: ValoraTypography.bodyMedium.copyWith(color: Colors.white),
    shape: RoundedRectangleBorder(
      borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
    ),
    insetPadding: const EdgeInsets.all(ValoraSpacing.md),
  );

  static SnackBarThemeData get _darkSnackBarTheme => SnackBarThemeData(
    behavior: SnackBarBehavior.floating,
    backgroundColor: ValoraColors.neutral200,
    contentTextStyle: ValoraTypography.bodyMedium.copyWith(
      color: ValoraColors.neutral900,
    ),
    shape: RoundedRectangleBorder(
      borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
    ),
    insetPadding: const EdgeInsets.all(ValoraSpacing.md),
  );

  // ============================================
  // TOOLTIP THEME
  // ============================================

  static TooltipThemeData get _lightTooltipTheme => TooltipThemeData(
    decoration: BoxDecoration(
      color: ValoraColors.neutral800,
      borderRadius: BorderRadius.circular(ValoraSpacing.radiusSm),
    ),
    textStyle: ValoraTypography.labelSmall.copyWith(color: Colors.white),
    padding: const EdgeInsets.symmetric(
      horizontal: ValoraSpacing.sm,
      vertical: ValoraSpacing.xs,
    ),
  );

  static TooltipThemeData get _darkTooltipTheme => TooltipThemeData(
    decoration: BoxDecoration(
      color: ValoraColors.neutral200,
      borderRadius: BorderRadius.circular(ValoraSpacing.radiusSm),
    ),
    textStyle: ValoraTypography.labelSmall.copyWith(
      color: ValoraColors.neutral900,
    ),
    padding: const EdgeInsets.symmetric(
      horizontal: ValoraSpacing.sm,
      vertical: ValoraSpacing.xs,
    ),
  );

  // ============================================
  // PROGRESS INDICATOR THEME
  // ============================================

  static ProgressIndicatorThemeData get _progressTheme =>
      ProgressIndicatorThemeData(
        linearTrackColor: ValoraColors.neutral200,
        color: ValoraColors.primary,
        circularTrackColor: ValoraColors.neutral200,
      );
}
