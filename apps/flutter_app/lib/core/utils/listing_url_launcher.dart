import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';
import '../theme/valora_colors.dart';

class ListingUrlLauncher {
  static Future<void> openExternalLink(BuildContext context, String? url) async {
    if (url != null) {
      await _openUrl(context, url);
    }
  }

  static Future<void> openMap(BuildContext context, double? latitude, double? longitude, String address, String? city) async {
    final Uri uri;
    if (latitude != null && longitude != null) {
      uri = Uri.parse(
        'https://www.google.com/maps/search/?api=1&query=$latitude,$longitude',
      );
    } else {
      final String query = '$address ${city ?? ''}'.trim();
      uri = Uri.parse(
        'https://www.google.com/maps/search/?api=1&query=${Uri.encodeComponent(query)}',
      );
    }

    await _openUrl(context, uri.toString());
  }

  static Future<void> openVirtualTour(BuildContext context, String? url) async {
    if (url != null) {
      await _openUrl(context, url);
    }
  }

  static Future<void> openVideo(BuildContext context, String? url) async {
    if (url != null) {
      await _openUrl(context, url);
    }
  }

  static Future<void> openFirstFloorPlan(BuildContext context, List<String> urls) async {
    if (urls.isNotEmpty) {
      await _openUrl(context, urls.first);
    }
  }

  static Future<void> contactBroker(BuildContext context, String? phone) async {
    if (phone != null) {
      final uri = Uri.parse('tel:${phone.replaceAll(RegExp(r'[^0-9+]'), '')}');
      try {
        if (!await launchUrl(uri)) {
          if (context.mounted) {
            _showErrorSnackBar(context, 'Could not launch dialer');
          }
        }
      } catch (e) {
        if (context.mounted) {
          _showErrorSnackBar(context, 'Error launching dialer: $e');
        }
      }
    }
  }

  static Future<void> _openUrl(BuildContext context, String url) async {
    final Uri uri = Uri.parse(url);
    try {
      if (!await launchUrl(uri, mode: LaunchMode.externalApplication) &&
          context.mounted) {
        _showErrorSnackBar(context, 'Could not open $url');
      }
    } catch (e) {
      if (context.mounted) {
        _showErrorSnackBar(context, 'Error launching URL: $e');
      }
    }
  }

  static void _showErrorSnackBar(BuildContext context, String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(message), backgroundColor: ValoraColors.error),
    );
  }
}
