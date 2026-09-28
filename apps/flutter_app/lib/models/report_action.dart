import 'package:flutter/material.dart';

enum ActionType {
  comparison,
  save,
  map,
  ai,
}

class ReportAction {
  const ReportAction({
    required this.id,
    required this.title,
    required this.description,
    required this.icon,
    required this.type,
  });

  final String id;
  final String title;
  final String description;
  final IconData icon;
  final ActionType type;

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is ReportAction &&
          runtimeType == other.runtimeType &&
          id == other.id;

  @override
  int get hashCode => id.hashCode;
}
