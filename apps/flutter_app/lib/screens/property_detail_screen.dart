import 'package:flutter/material.dart';
import '../models/property_detail.dart';
import '../services/api_service.dart';
import '../widgets/common/valora_loading_indicator.dart';
import '../widgets/property/property_header.dart';
import '../widgets/property/property_stats.dart';
import '../widgets/property/score_drivers.dart';
import '../widgets/property/valuation_card.dart';
import '../widgets/property/nearby_amenities.dart';
import '../widgets/valora_error_state.dart';

class PropertyDetailScreen extends StatefulWidget {
  final String propertyId;
  final ApiService? apiService;

  const PropertyDetailScreen({
    super.key,
    required this.propertyId,
    this.apiService,
  });

  @override
  State<PropertyDetailScreen> createState() => _PropertyDetailScreenState();
}

class _PropertyDetailScreenState extends State<PropertyDetailScreen> {
  late Future<PropertyDetail> _propertyFuture;
  late final ApiService _apiService;

  @override
  void initState() {
    super.initState();
    _apiService = widget.apiService ?? ApiService();
    _propertyFuture = _apiService.getPropertyDetail(widget.propertyId);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: FutureBuilder<PropertyDetail>(
        future: _propertyFuture,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const Center(child: ValoraLoadingIndicator());
          }

          if (snapshot.hasError) {
            return Scaffold(
              appBar: AppBar(),
              body: ValoraErrorState(
                error: snapshot.error ?? Exception('Failed to load'),
                onRetry: () {
                  setState(() {
                    _propertyFuture = _apiService.getPropertyDetail(widget.propertyId);
                  });
                },
              ),
            );
          }

          if (!snapshot.hasData) {
             return Scaffold(
              appBar: AppBar(),
              body: const Center(child: Text("Property not found")),
            );
          }

          final property = snapshot.data!;

          return CustomScrollView(
            slivers: [
              SliverAppBar(
                expandedHeight: 250.0,
                pinned: true,
                flexibleSpace: FlexibleSpaceBar(
                  background: property.imageUrls.isNotEmpty
                      ? Image.network(
                          property.imageUrls.first,
                          fit: BoxFit.cover,
                          errorBuilder: (context, error, stackTrace) =>
                              Container(color: Colors.grey[300]),
                        )
                      : Container(color: Colors.grey[300]),
                ),
              ),
              SliverToBoxAdapter(
                child: Column(
                  children: [
                    PropertyHeader(property: property),
                    const SizedBox(height: 16),
                    PropertyStats(property: property),
                    ScoreDrivers(property: property),
                    ValuationCard(property: property),
                    NearbyAmenities(property: property),
                    if (property.description != null)
                      Padding(
                        padding: const EdgeInsets.all(16.0),
                        child: Text(
                          property.description!,
                          style: Theme.of(context).textTheme.bodyMedium,
                        ),
                      ),
                    const SizedBox(height: 32),
                  ],
                ),
              ),
            ],
          );
        },
      ),
    );
  }
}
