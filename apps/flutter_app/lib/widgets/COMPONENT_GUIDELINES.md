# Component Guidelines

Usage documentation for Valora's reusable widgets.

---

## ValoraCard

General-purpose container with consistent styling.

### Usage

```dart
ValoraCard(
  child: Text('Content'),
  onTap: () => print('Tapped'),
)
```

### Props
| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `child` | Widget | required | Card content |
| `padding` | EdgeInsets? | 16 | Inner padding |
| `onTap` | VoidCallback? | null | Makes card tappable |
| `elevation` | double? | 1 | Shadow depth |

---

## ValoraButton

Button with multiple variants.

### Variants

```dart
ValoraButton(
  label: 'Submit',
  variant: ValoraButtonVariant.primary,
  onPressed: () {},
)
```

- `primary` - Filled, brand color
- `secondary` - Subtle fill
- `outline` - Border only
- `ghost` - Text only

---

## ValoraListingCard

Property listing card with image and details.

### Usage

```dart
ValoraListingCard(
  listing: myListing,
  onTap: () => navigateToDetails(myListing),
  onFavorite: () => toggleFavorite(myListing),
  isFavorite: favorites.contains(myListing.id),
)
```

### Features
- Responsive image with loading/error states
- Price with proper formatting
- Status badge (New, Sold, etc.)
- Favorite toggle
- Specs row (bedrooms, bathrooms, area)

---

## ValoraEmptyState

Empty/error state placeholder.

### Usage

```dart
ValoraEmptyState(
  icon: Icons.search_off,
  title: 'No results found',
  subtitle: 'Try adjusting your filters',
  action: ValoraButton(
    label: 'Clear filters',
    onPressed: clearFilters,
  ),
)
```

---

## ValoraPrice

Formatted price display.

```dart
ValoraPrice(
  price: 450000,
  size: ValoraPriceSize.medium,
)
// Output: € 450.000
```

---

## ValoraBadge

Status indicator badge.

```dart
ValoraBadge(
  label: 'NEW',
  color: ValoraColors.newBadge,
  icon: Icons.star,
)
```

---

## Best Practices

1. **Use semantic components** - Prefer `ValoraListingCard` over custom Card layouts
2. **Respect spacing** - Use `ValoraSpacing` constants between components
3. **Use theme colors** - Access via `Theme.of(context).colorScheme`
4. **Keep states consistent** - Loading, error, empty states should use standard widgets
