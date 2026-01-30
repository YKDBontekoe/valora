# Valora Design System

A comprehensive design system for the Valora real estate application built with Flutter and Material Design 3.

## Design Principles

### 1. Consistency
Use design tokens instead of hardcoded values. All colors, typography, and spacing should come from the centralized system.

### 2. Accessibility
- All color combinations meet WCAG AA contrast requirements (4.5:1 for normal text, 3:1 for large text)
- Touch targets are minimum 48x48dp
- Text is scalable with system font size

### 3. Scalability
Token-based architecture allows global updates by changing single values.

### 4. Modern Aesthetics
Premium feel with subtle shadows, smooth animations, and cohesive visual language.

---

## Color System

### Usage Rules

| Token | When to Use |
|-------|-------------|
| `primary` | Main brand actions, active states, links |
| `accent` | CTAs, highlights, attention-grabbing elements |
| `surface` | Card backgrounds, containers |
| `onSurface` | Primary text on surfaces |
| `onSurfaceVariant` | Secondary/muted text |
| `error` | Error states, destructive actions |
| `success` | Success states, positive indicators |
| `priceTag` | Property price display |

### ❌ Avoid
```dart
// Bad - hardcoded colors
Container(color: Color(0xFF6366F1))
Text(style: TextStyle(color: Colors.grey[600]))

// Good - use semantic tokens
Container(color: ValoraColors.primary)
Text(style: TextStyle(color: Theme.of(context).colorScheme.onSurfaceVariant))
```

---

## Typography

### Hierarchy

| Style | Usage |
|-------|-------|
| `displayLarge` | Hero headlines |
| `headlineLarge` | Page titles |
| `titleLarge` | Card titles, section headers |
| `titleMedium` | Listing addresses |
| `bodyLarge` | Primary body text |
| `bodyMedium` | Secondary body text |
| `labelLarge` | Buttons, prominent labels |
| `labelSmall` | Metadata, captions |
| `priceDisplay` | Property prices |

### Rules
1. Use `Theme.of(context).textTheme` for standard styles
2. Use `ValoraTypography` for custom domain styles (prices, addresses)
3. Never hardcode font sizes or weights

---

## Spacing

### 8px Grid System

| Token | Value | Usage |
|-------|-------|-------|
| `xs` | 4px | Inline gaps, tight spacing |
| `sm` | 8px | Icon-to-text gaps, small margins |
| `md` | 16px | Card padding, list item gaps, screen edges |
| `lg` | 24px | Section spacing |
| `xl` | 32px | Major section breaks |
| `xxl` | 48px | Page-level spacing |

### Rules
1. Always use spacing tokens from `ValoraSpacing`
2. Maintain consistent padding within components
3. Use `ValoraSpacing.screenPadding` (16px) for screen edges

---

## Theming

### Applying Themes

```dart
MaterialApp(
  theme: ValoraTheme.light,
  darkTheme: ValoraTheme.dark,
  themeMode: ThemeMode.system,
);
```

### Accessing Theme Values

```dart
// Colors
Theme.of(context).colorScheme.primary
Theme.of(context).colorScheme.surface

// Typography
Theme.of(context).textTheme.titleLarge

// Direct tokens (when theme context unavailable)
ValoraColors.primary
ValoraTypography.priceDisplay
ValoraSpacing.md
```

---

## File Structure

```
lib/core/theme/
├── DESIGN_SYSTEM.md     # This file
├── valora_colors.dart   # Color tokens
├── valora_typography.dart # Type scale
├── valora_spacing.dart  # Spacing & sizing
└── valora_theme.dart    # ThemeData configs
```
