import 'package:flutter/material.dart';
import 'package:cached_network_image/cached_network_image.dart';
import '../models/listing_detail.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_typography.dart';
import '../core/theme/valora_spacing.dart';

class PropertyDetailScreen extends StatelessWidget {
  final ListingDetail listing;

  const PropertyDetailScreen({super.key, required this.listing});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: ValoraColors.neutral50,
      body: CustomScrollView(
        slivers: [
          SliverAppBar(
            expandedHeight: 300,
            pinned: true,
            flexibleSpace: FlexibleSpaceBar(
              background: listing.imageUrl != null
                  ? CachedNetworkImage(
                      imageUrl: listing.imageUrl!,
                      fit: BoxFit.cover,
                    )
                  : Container(color: ValoraColors.neutral200),
            ),
          ),
          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.all(ValoraSpacing.md),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    listing.address,
                    style: ValoraTypography.headlineMedium,
                  ),
                  if (listing.price != null)
                    Text(
                      '€${listing.price!.toStringAsFixed(0)}',
                      style: ValoraTypography.headlineSmall.copyWith(color: ValoraColors.primary),
                    ),
                  const SizedBox(height: ValoraSpacing.lg),
                  _buildFacts(),
                  const SizedBox(height: ValoraSpacing.lg),
                  if (listing.contextCompositeScore != null) ...[
                    Text('Context Score Breakdown', style: ValoraTypography.titleLarge),
                    const SizedBox(height: ValoraSpacing.sm),
                    _buildContextScores(),
                  ],
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildFacts() {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceAround,
      children: [
        if (listing.bedrooms != null) _buildFactItem(Icons.bed, '${listing.bedrooms} Beds'),
        if (listing.bathrooms != null) _buildFactItem(Icons.bathtub, '${listing.bathrooms} Baths'),
        if (listing.livingAreaM2 != null) _buildFactItem(Icons.square_foot, '${listing.livingAreaM2} m²'),
      ],
    );
  }

  Widget _buildFactItem(IconData icon, String text) {
    return Column(
      children: [
        Icon(icon, color: ValoraColors.neutral500),
        const SizedBox(height: ValoraSpacing.xs),
        Text(text, style: ValoraTypography.bodyMedium),
      ],
    );
  }

  Widget _buildContextScores() {
    return Column(
      children: [
        if (listing.contextCompositeScore != null)
          _buildScoreRow('Composite', listing.contextCompositeScore!),
        if (listing.contextSafetyScore != null)
          _buildScoreRow('Safety', listing.contextSafetyScore!),
        if (listing.contextSocialScore != null)
          _buildScoreRow('Social', listing.contextSocialScore!),
        if (listing.contextAmenitiesScore != null)
          _buildScoreRow('Amenities', listing.contextAmenitiesScore!),
        if (listing.contextEnvironmentScore != null)
          _buildScoreRow('Environment', listing.contextEnvironmentScore!),
      ],
    );
  }

  Widget _buildScoreRow(String label, double score) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: ValoraSpacing.xs),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: ValoraTypography.bodyMedium),
          Text(score.toStringAsFixed(1), style: ValoraTypography.bodyLarge.copyWith(fontWeight: FontWeight.bold)),
        ],
      ),
    );
  }
}
