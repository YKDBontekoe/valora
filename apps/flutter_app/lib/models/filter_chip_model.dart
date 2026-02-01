import 'package:flutter/material.dart';

enum HomeQuickFilter {
  aiPick,
  under500k,
  threePlusBeds,
  nearSchools,
}

class ValoraFilterChipModel {
  final HomeQuickFilter filter;
  final String label;
  final IconData? icon;
  final bool isActive;

  const ValoraFilterChipModel({
    required this.filter,
    required this.label,
    this.icon,
    required this.isActive,
  });

  ValoraFilterChipModel copyWith({
    bool? isActive,
  }) {
    return ValoraFilterChipModel(
      filter: filter,
      label: label,
      icon: icon,
      isActive: isActive ?? this.isActive,
    );
  }
}
