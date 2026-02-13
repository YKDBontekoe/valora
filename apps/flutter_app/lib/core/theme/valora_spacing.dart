/// Valora Design System - Spacing & Layout
///
/// An 8px grid system for consistent spacing throughout the app.
/// All spacing values are multiples of the base unit (8px).
abstract final class ValoraSpacing {
  // ============================================
  // BASE UNIT
  // ============================================

  /// Base spacing unit (8px)
  static const double unit = 8.0;

  // ============================================
  // SPACING SCALE
  // ============================================

  /// Extra small - 4px (0.5x)
  static const double xs = 4.0;

  /// Small - 8px (1x)
  static const double sm = 8.0;

  /// Medium - 16px (2x)
  static const double md = 16.0;

  /// Large - 24px (3x)
  static const double lg = 24.0;

  /// Extra large - 32px (4x)
  static const double xl = 32.0;

  /// Extra extra large - 48px (6x)
  static const double xxl = 48.0;

  /// Extra extra extra large - 64px (8x)
  static const double xxxl = 64.0;

  // ============================================
  // COMPONENT SPACING
  // ============================================

  /// Padding inside cards
  static const double cardPadding = md;

  /// Gap between list items
  static const double listItemGap = md;

  /// Screen edge padding
  static const double screenPadding = md;

  /// Section spacing
  static const double sectionGap = lg;

  /// Icon-to-text gap
  static const double iconGap = sm;

  /// Inline element gap
  static const double inlineGap = xs;

  // ============================================
  // BORDER RADIUS
  // ============================================

  /// No radius
  static const double radiusNone = 0.0;

  /// Small radius - subtle rounding
  static const double radiusSm = 4.0;

  /// Medium radius - standard components
  static const double radiusMd = 8.0;

  /// Large radius - cards, modals
  static const double radiusLg = 12.0;

  /// Extra large radius - floating elements
  static const double radiusXl = 16.0;

  /// Extra extra large radius - prominent cards
  static const double radiusXxl = 24.0;

  /// Full/circular radius
  static const double radiusFull = 9999.0;

  // ============================================
  // ELEVATION / SHADOWS
  // ============================================

  /// No elevation
  static const double elevationNone = 0.0;

  /// Low elevation - subtle lift
  static const double elevationSm = 1.0;

  /// Medium elevation - cards
  static const double elevationMd = 2.0;

  /// High elevation - modals, FABs
  static const double elevationLg = 4.0;

  /// Extra high elevation - dropdowns
  static const double elevationXl = 8.0;

  // ============================================
  // SIZING
  // ============================================

  /// Icon sizes
  static const double iconSizeSm = 16.0;
  static const double iconSizeMd = 24.0;
  static const double iconSizeLg = 32.0;
  static const double iconSizeXl = 48.0;

  /// Touch target minimum (accessibility)
  static const double touchTargetMin = 48.0;

  /// Avatar sizes
  static const double avatarSm = 32.0;
  static const double avatarMd = 40.0;
  static const double avatarLg = 56.0;

  /// Button heights
  static const double buttonHeightSm = 32.0;
  static const double buttonHeightMd = 44.0;
  static const double buttonHeightLg = 52.0;

  /// Image aspect ratios
  static const double listingImageHeight = 200.0;
  static const double listingCardImageHeight = 180.0;
  static const double thumbnailSize = 80.0;
  static const double thumbnailSizeLg = 96.0;
}
