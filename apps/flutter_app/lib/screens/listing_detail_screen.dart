import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_spacing.dart';
import '../core/theme/valora_typography.dart';
import '../models/listing.dart';
import '../widgets/valora_widgets.dart';

class ListingDetailScreen extends StatelessWidget {
  const ListingDetailScreen({
    super.key,
    required this.listing,
  });

  final Listing listing;

  Future<void> _openExternalLink(BuildContext context) async {
    final url = listing.url;
    if (url != null) {
      final uri = Uri.parse(url);
      try {
        if (!await launchUrl(uri, mode: LaunchMode.externalApplication)) {
          if (context.mounted) {
            _showErrorSnackBar(context, 'Could not open $url');
          }
        }
      } catch (e) {
        if (context.mounted) {
          _showErrorSnackBar(context, 'Error launching URL: $e');
        }
      }
    }
  }

  void _showErrorSnackBar(BuildContext context, String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(message),
        backgroundColor: ValoraColors.error,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final colorScheme = Theme.of(context).colorScheme;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Details'),
      ),
      body: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _buildImage(isDark),
            Padding(
              padding: const EdgeInsets.all(ValoraSpacing.screenPadding),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  _buildHeader(colorScheme),
                  const SizedBox(height: ValoraSpacing.lg),
                  _buildAddressSection(colorScheme),
                  const SizedBox(height: ValoraSpacing.lg),
                  const Divider(),
                  const SizedBox(height: ValoraSpacing.lg),
                  _buildSpecsGrid(context, colorScheme),
                  const SizedBox(height: ValoraSpacing.xl),
                  if (listing.url != null)
                    ValoraButton(
                      label: 'View on Funda',
                      icon: Icons.open_in_new,
                      isFullWidth: true,
                      onPressed: () => _openExternalLink(context),
                    ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildImage(bool isDark) {
    return Hero(
      tag: listing.id,
      child: AspectRatio(
        aspectRatio: 16 / 10,
        child: listing.imageUrl != null
            ? Image.network(
                listing.imageUrl!,
                fit: BoxFit.cover,
                errorBuilder: (context, error, stackTrace) =>
                    _buildPlaceholder(isDark),
                frameBuilder: (context, child, frame, wasSynchronouslyLoaded) {
                  if (wasSynchronouslyLoaded) return child;
                  return AnimatedOpacity(
                    opacity: frame == null ? 0 : 1,
                    duration: const Duration(milliseconds: 500),
                    curve: Curves.easeOut,
                    child: child,
                  );
                },
                loadingBuilder: (context, child, loadingProgress) {
                  if (loadingProgress == null) return child;
                  return _buildPlaceholder(isDark, isLoading: true);
                },
              )
            : _buildPlaceholder(isDark),
      ),
    );
  }

  Widget _buildPlaceholder(bool isDark, {bool isLoading = false}) {
    return Container(
      color: isDark ? ValoraColors.surfaceVariantDark : ValoraColors.neutral100,
      child: Center(
        child: isLoading
            ? const SizedBox(
                width: 32,
                height: 32,
                child: CircularProgressIndicator(strokeWidth: 3),
              )
            : Icon(
                Icons.home_outlined,
                size: ValoraSpacing.iconSizeXl * 1.5,
                color:
                    isDark ? ValoraColors.neutral500 : ValoraColors.neutral400,
              ),
      ),
    );
  }

  Widget _buildHeader(ColorScheme colorScheme) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (listing.price != null)
          ValoraPrice(
            price: listing.price!,
            size: ValoraPriceSize.large,
          ),
        if (listing.status != null)
          ValoraBadge(
            label: listing.status!.toUpperCase(),
            color: _getStatusColor(listing.status!),
          ),
      ],
    );
  }

  Widget _buildAddressSection(ColorScheme colorScheme) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          listing.address,
          style: ValoraTypography.headlineSmall.copyWith(
            color: colorScheme.onSurface,
          ),
        ),
        const SizedBox(height: ValoraSpacing.xs),
        Text(
          '${listing.city ?? ''} ${listing.postalCode ?? ''}'.trim(),
          style: ValoraTypography.bodyLarge.copyWith(
            color: colorScheme.onSurfaceVariant,
          ),
        ),
      ],
    );
  }

  Widget _buildSpecsGrid(BuildContext context, ColorScheme colorScheme) {
    final specs = <Widget>[];

    if (listing.bedrooms != null) {
      specs.add(_buildSpecItem(
        Icons.bed_outlined,
        'Bedrooms',
        '${listing.bedrooms}',
        colorScheme,
      ));
    }

    if (listing.bathrooms != null) {
      specs.add(_buildSpecItem(
        Icons.bathtub_outlined,
        'Bathrooms',
        '${listing.bathrooms}',
        colorScheme,
      ));
    }

    if (listing.livingAreaM2 != null) {
      specs.add(_buildSpecItem(
        Icons.square_foot_outlined,
        'Living Area',
        '${listing.livingAreaM2} m²',
        colorScheme,
      ));
    }

    if (listing.plotAreaM2 != null) {
      specs.add(_buildSpecItem(
        Icons.landscape_outlined,
        'Plot Size',
        '${listing.plotAreaM2} m²',
        colorScheme,
      ));
    }

    if (specs.isEmpty) return const SizedBox.shrink();

    return GridView.count(
      crossAxisCount: 2,
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      childAspectRatio: 2.5,
      mainAxisSpacing: ValoraSpacing.md,
      crossAxisSpacing: ValoraSpacing.md,
      children: specs,
    );
  }

  Widget _buildSpecItem(
    IconData icon,
    String label,
    String value,
    ColorScheme colorScheme,
  ) {
    return Row(
      children: [
        Container(
          padding: const EdgeInsets.all(ValoraSpacing.sm),
          decoration: BoxDecoration(
            color: colorScheme.surfaceContainerHighest,
            borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
          ),
          child: Icon(
            icon,
            size: ValoraSpacing.iconSizeMd,
            color: colorScheme.primary,
          ),
        ),
        const SizedBox(width: ValoraSpacing.md),
        Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Text(
              label,
              style: ValoraTypography.labelSmall.copyWith(
                color: colorScheme.onSurfaceVariant,
              ),
            ),
            Text(
              value,
              style: ValoraTypography.titleMedium.copyWith(
                color: colorScheme.onSurface,
              ),
            ),
          ],
        ),
      ],
    );
  }

  Color _getStatusColor(String status) {
    switch (status.toLowerCase()) {
      case 'new':
        return ValoraColors.newBadge;
      case 'sold':
      case 'under offer':
        return ValoraColors.soldBadge;
      default:
        return ValoraColors.primary;
    }
  }
}
