import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_spacing.dart';
import '../models/listing.dart';
import '../widgets/valora_glass_container.dart';

class PropertyDetailsScreen extends StatelessWidget {
  final Listing listing;

  const PropertyDetailsScreen({super.key, required this.listing});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Theme.of(context).scaffoldBackgroundColor,
      body: Stack(
        children: [
          CustomScrollView(
            slivers: [
              _buildSliverAppBar(context),
              SliverToBoxAdapter(
                child: Padding(
                  padding: const EdgeInsets.symmetric(horizontal: ValoraSpacing.md),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const SizedBox(height: ValoraSpacing.md),
                      _buildHeader(context),
                      const SizedBox(height: ValoraSpacing.md),
                      _buildSpecs(context),
                      const SizedBox(height: ValoraSpacing.xl),
                      _buildSentimentSection(context),
                      const SizedBox(height: ValoraSpacing.xl),
                      _buildPriceHistorySection(context),
                      const SizedBox(height: ValoraSpacing.xl),
                      _buildMarketComparisonSection(context),
                      const SizedBox(height: 120), // Spacer for bottom buttons
                    ],
                  ),
                ),
              ),
            ],
          ),
          Positioned(
            bottom: 24,
            left: 24,
            right: 24,
            child: _buildBottomActions(context),
          ),
        ],
      ),
    );
  }

  Widget _buildSliverAppBar(BuildContext context) {
    return SliverAppBar(
      expandedHeight: 320,
      pinned: true,
      backgroundColor: Colors.transparent,
      leading: Padding(
        padding: const EdgeInsets.all(8.0),
        child: CircleAvatar(
          backgroundColor: Colors.white.withValues(alpha: 0.7),
          child: IconButton(
            icon: const Icon(Icons.arrow_back, color: ValoraColors.neutral900),
            onPressed: () => Navigator.pop(context),
          ),
        ),
      ),
      actions: [
        Padding(
          padding: const EdgeInsets.all(8.0),
          child: CircleAvatar(
            backgroundColor: Colors.white.withValues(alpha: 0.7),
            child: IconButton(
              icon: const Icon(Icons.favorite_border, color: ValoraColors.primary),
              onPressed: () {},
            ),
          ),
        ),
      ],
      flexibleSpace: FlexibleSpaceBar(
        background: Stack(
          fit: StackFit.expand,
          children: [
            listing.imageUrl != null
                ? CachedNetworkImage(
                    imageUrl: listing.imageUrl!,
                    fit: BoxFit.cover,
                  )
                : Container(color: Colors.grey),
            Positioned(
              top: 100, // Adjust based on design, seemingly top right but overlaying image
              right: 16,
              child: ValoraGlassContainer(
                padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                borderRadius: BorderRadius.circular(16),
                child: Text(
                  listing.price != null
                      ? '\$${listing.price!.toStringAsFixed(0).replaceAllMapped(RegExp(r'(\d{1,3})(?=(\d{3})+(?!\d))'), (Match m) => '${m[1]},')}'
                      : 'Price on Request',
                  style: const TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                    color: ValoraColors.primary,
                  ),
                ),
              ),
            ),
             Positioned(
              bottom: 16,
              left: 16,
              child: ValoraGlassContainer(
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                borderRadius: BorderRadius.circular(12),
                child: Row(
                  children: [
                    const Icon(Icons.auto_awesome, size: 16, color: ValoraColors.primary),
                    const SizedBox(width: 8),
                    const Text(
                      'Underpriced by 4%',
                      style: TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.w600,
                        color: ValoraColors.neutral900,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildHeader(BuildContext context) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                listing.address, // Using address as title as per design suggestion if name not available
                style: Theme.of(context).textTheme.headlineMedium?.copyWith(
                      fontWeight: FontWeight.bold,
                    ),
              ),
              const SizedBox(height: 4),
              Row(
                children: [
                  const Icon(Icons.location_on, size: 16, color: Colors.grey),
                  const SizedBox(width: 4),
                  Text(
                    '${listing.city}, ${listing.postalCode}',
                    style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                          color: Colors.grey,
                        ),
                  ),
                ],
              ),
            ],
          ),
        ),
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
          decoration: BoxDecoration(
            color: Colors.yellow[100],
            borderRadius: BorderRadius.circular(8),
          ),
          child: Row(
            children: [
              const Icon(Icons.star, size: 16, color: Colors.amber),
              const SizedBox(width: 4),
              const Text(
                '4.9',
                style: TextStyle(
                  fontWeight: FontWeight.bold,
                  color: Colors.amber, // Darker amber for text
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildSpecs(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 16),
      decoration: const BoxDecoration(
        border: Border(
          bottom: BorderSide(color: Colors.grey, width: 0.2),
        ),
      ),
      child: Row(
        children: [
          _buildSpecItem(Icons.bed_outlined, '${listing.bedrooms ?? 0} Beds'),
          const SizedBox(width: 24),
          _buildSpecItem(Icons.bathtub_outlined, '${listing.bathrooms ?? 0} Baths'),
          const SizedBox(width: 24),
          _buildSpecItem(Icons.square_foot, '${listing.livingAreaM2 ?? 0} sqft'),
        ],
      ),
    );
  }

  Widget _buildSpecItem(IconData icon, String label) {
    return Row(
      children: [
        Icon(icon, color: Colors.grey, size: 20),
        const SizedBox(width: 8),
        Text(
          label,
          style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 14),
        ),
      ],
    );
  }

  Widget _buildSentimentSection(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            const Text(
              'AI Neighborhood Sentiment',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
              decoration: BoxDecoration(
                color: ValoraColors.primary.withValues(alpha: 0.1),
                borderRadius: BorderRadius.circular(12),
              ),
              child: const Text(
                'Beta',
                style: TextStyle(
                  color: ValoraColors.primary,
                  fontSize: 12,
                  fontWeight: FontWeight.w500,
                ),
              ),
            ),
          ],
        ),
        const SizedBox(height: 16),
        Row(
          children: [
            Expanded(child: _buildSentimentCard('Safety', '9.8', Icons.security, Colors.green)),
            const SizedBox(width: 12),
            Expanded(child: _buildSentimentCard('Quiet', '8.5', Icons.graphic_eq, Colors.blue)),
            const SizedBox(width: 12),
            Expanded(child: _buildSentimentCard('Nature', '9.2', Icons.park, Colors.green)), // Changed to Colors.green
          ],
        ),
      ],
    );
  }

  Widget _buildSentimentCard(String title, String score, IconData icon, MaterialColor color) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: Colors.grey.withValues(alpha: 0.2)),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.05),
            blurRadius: 4,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        children: [
          Container(
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(
              color: color[50],
              shape: BoxShape.circle,
            ),
            child: Icon(icon, color: color, size: 24),
          ),
          const SizedBox(height: 8),
          Text(title, style: const TextStyle(color: Colors.grey, fontSize: 12)),
          const SizedBox(height: 4),
          Text(score, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 18)),
        ],
      ),
    );
  }

  Widget _buildPriceHistorySection(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Price History & Forecast',
          style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 16),
        Container(
          padding: const EdgeInsets.all(20),
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(16),
            border: Border.all(color: Colors.grey.withValues(alpha: 0.2)),
          ),
          child: Column(
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text('Estimated Value in 2025', style: TextStyle(color: Colors.grey, fontSize: 12)),
                      Row(
                        children: [
                          const Text('~\$2.6M', style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: ValoraColors.primary)),
                          const SizedBox(width: 4),
                          Text('▲ 5.2%', style: TextStyle(fontSize: 12, color: Colors.green[600])),
                        ],
                      ),
                    ],
                  ),
                  Row(
                    children: [
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                        decoration: BoxDecoration(color: ValoraColors.primary, borderRadius: BorderRadius.circular(6)),
                        child: const Text('1Y', style: TextStyle(color: Colors.white, fontSize: 12)),
                      ),
                      const SizedBox(width: 8),
                      const Text('5Y', style: TextStyle(color: Colors.grey, fontSize: 12)),
                    ],
                  ),
                ],
              ),
              const SizedBox(height: 24),
              SizedBox(
                height: 120,
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    _buildChartBar(0.4, Colors.indigo[100]!),
                    _buildChartBar(0.55, Colors.indigo[200]!),
                    _buildChartBar(0.45, Colors.indigo[300]!),
                    _buildChartBar(0.6, Colors.indigo[400]!),
                    _buildChartBar(0.75, Colors.indigo[500]!),
                    _buildChartBar(0.85, ValoraColors.primary, isCurrent: true),
                    _buildChartBar(0.95, ValoraColors.primary.withValues(alpha: 0.5), isDashed: true),
                  ],
                ),
              ),
              const SizedBox(height: 8),
              const Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text('Jan', style: TextStyle(fontSize: 10, color: Colors.grey)),
                  Text('Mar', style: TextStyle(fontSize: 10, color: Colors.grey)),
                  Text('May', style: TextStyle(fontSize: 10, color: Colors.grey)),
                  Text('Jul', style: TextStyle(fontSize: 10, color: Colors.grey)),
                  Text('Sep', style: TextStyle(fontSize: 10, color: Colors.grey)),
                  Text('Now', style: TextStyle(fontSize: 10, color: Colors.grey)),
                  Text('2025', style: TextStyle(fontSize: 10, color: Colors.grey)),
                ],
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildChartBar(double heightFactor, Color color, {bool isCurrent = false, bool isDashed = false}) {
    return Expanded(
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 4),
        child: Stack(
          alignment: Alignment.bottomCenter,
          clipBehavior: Clip.none,
          children: [
            if (isDashed)
              Container(
                decoration: BoxDecoration(
                   border: Border.all(color: color, width: 1.5, style: BorderStyle.solid), // Dashed border needs custom painter, simpler to use solid or opacity for now
                   borderRadius: const BorderRadius.vertical(top: Radius.circular(4)),
                   color: color.withValues(alpha: 0.2),
                ),
                height: 120 * heightFactor,
              )
            else
              Container(
                decoration: BoxDecoration(
                  color: color,
                  borderRadius: const BorderRadius.vertical(top: Radius.circular(4)),
                   boxShadow: isCurrent ? [BoxShadow(color: color.withValues(alpha: 0.5), blurRadius: 10, spreadRadius: 1)] : null,
                ),
                height: 120 * heightFactor,
              ),
            if (isCurrent)
              Positioned(
                top: -4,
                child: Container(
                  width: 8,
                  height: 8,
                  decoration: const BoxDecoration(color: Colors.white, shape: BoxShape.circle),
                ),
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildMarketComparisonSection(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Local Market Comparison',
          style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 16),
        Container(
          padding: const EdgeInsets.all(20),
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(16),
            border: Border.all(color: Colors.grey.withValues(alpha: 0.2)),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text('How this home stacks up against local averages.', style: TextStyle(color: Colors.grey, fontSize: 14)),
              const SizedBox(height: 16),
              _buildComparisonBar('Price per SqFt', 'Great Value', Colors.green, 0.4, 0.6),
              const SizedBox(height: 16),
              _buildComparisonBar('Property Tax', 'Above Average', Colors.orange, 0.75, 0.5),
              const SizedBox(height: 24),
              SizedBox(
                width: double.infinity,
                child: OutlinedButton(
                  onPressed: () {},
                  style: OutlinedButton.styleFrom(
                    side: const BorderSide(color: ValoraColors.primary),
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                    padding: const EdgeInsets.symmetric(vertical: 16),
                  ),
                  child: const Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(Icons.analytics, color: ValoraColors.primary),
                      SizedBox(width: 8),
                      Text('Full Comparison Report', style: TextStyle(color: ValoraColors.primary, fontWeight: FontWeight.bold)),
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildComparisonBar(String label, String status, Color statusColor, double valueFactor, double avgFactor) {
    return Column(
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(label, style: const TextStyle(fontWeight: FontWeight.w500)),
            Text(status, style: TextStyle(color: statusColor, fontWeight: FontWeight.bold)),
          ],
        ),
        const SizedBox(height: 8),
        SizedBox(
          height: 8,
          child: LayoutBuilder(
            builder: (context, constraints) {
              return Stack(
                alignment: Alignment.centerLeft,
                children: [
                  Container(
                    width: double.infinity,
                    height: 8,
                    decoration: BoxDecoration(color: Colors.grey[100], borderRadius: BorderRadius.circular(4)),
                  ),
                  Container(
                    width: constraints.maxWidth * valueFactor,
                    height: 8,
                    decoration: BoxDecoration(color: statusColor, borderRadius: BorderRadius.circular(4)),
                  ),
                  Positioned(
                    left: constraints.maxWidth * avgFactor,
                    child: Container(
                      width: 4,
                      height: 12,
                      decoration: BoxDecoration(color: Colors.grey, borderRadius: BorderRadius.circular(2)),
                    ),
                  ),
                ],
              );
            },
          ),
        ),
        const SizedBox(height: 4),
        const Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text('This Home', style: TextStyle(fontSize: 10, color: Colors.grey)),
            Text('Avg', style: TextStyle(fontSize: 10, color: Colors.grey)),
          ],
        ),
      ],
    );
  }

  Widget _buildBottomActions(BuildContext context) {
    return Row(
      children: [
        Expanded(
          flex: 1,
          child: ElevatedButton(
            onPressed: () {},
            style: ElevatedButton.styleFrom(
              backgroundColor: Colors.white,
              foregroundColor: Colors.black,
              padding: const EdgeInsets.symmetric(vertical: 16),
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16), side: BorderSide(color: Colors.grey.withValues(alpha: 0.2))),
              elevation: 4,
              shadowColor: Colors.black.withValues(alpha: 0.2),
            ),
            child: const Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Icon(Icons.chat_bubble_outline, color: ValoraColors.primary),
                SizedBox(width: 8),
                Text('AI Chat', style: TextStyle(fontWeight: FontWeight.bold)),
              ],
            ),
          ),
        ),
        const SizedBox(width: 16),
        Expanded(
          flex: 2,
          child: ElevatedButton(
            onPressed: () {},
            style: ElevatedButton.styleFrom(
              backgroundColor: ValoraColors.primary,
              foregroundColor: Colors.white,
              padding: const EdgeInsets.symmetric(vertical: 16),
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
              elevation: 4,
               shadowColor: ValoraColors.primary.withValues(alpha: 0.3),
            ),
            child: const Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Text('Book Viewing', style: TextStyle(fontWeight: FontWeight.bold)),
                SizedBox(width: 8),
                Icon(Icons.arrow_forward, size: 16),
              ],
            ),
          ),
        ),
      ],
    );
  }
}
