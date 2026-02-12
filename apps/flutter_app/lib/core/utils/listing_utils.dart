import 'package:flutter/material.dart';
import '../theme/valora_colors.dart';

class ListingUtils {
  static Color getStatusColor(String status) {
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

  static Color getScoreColor(double score) {
    if (score >= 80.0) return ValoraColors.success;
    if (score >= 60.0) return ValoraColors.primary;
    if (score >= 40.0) return ValoraColors.warning;
    return ValoraColors.error;
  }
}
