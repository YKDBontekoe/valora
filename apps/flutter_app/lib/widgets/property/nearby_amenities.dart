import 'package:flutter/material.dart';
import '../../models/property_detail.dart';
import '../common/valora_card.dart';
import '../../core/utils/map_utils.dart';
import '../../core/theme/valora_colors.dart';

class NearbyAmenities extends StatelessWidget {
  final PropertyDetail property;

  const NearbyAmenities({super.key, required this.property});

  @override
  Widget build(BuildContext context) {
    if (property.nearbyAmenities.isEmpty) return const SizedBox.shrink();

    return Padding(
      padding: const EdgeInsets.all(16.0),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Nearby Amenities', style: Theme.of(context).textTheme.titleLarge),
          const SizedBox(height: 8),
          ValoraCard(
            padding: EdgeInsets.zero,
            child: ListView.separated(
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              itemCount: property.nearbyAmenities.length,
              separatorBuilder: (context, index) => const Divider(height: 1),
              itemBuilder: (context, index) {
                final amenity = property.nearbyAmenities[index];
                return ListTile(
                  leading: Icon(
                    MapUtils.getAmenityIcon(amenity.type),
                    color: ValoraColors.primary,
                  ),
                  title: Text(amenity.name),
                  subtitle: Text(amenity.type),
                  dense: true,
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}
