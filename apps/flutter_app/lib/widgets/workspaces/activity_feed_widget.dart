import 'package:flutter/material.dart';
import '../../models/activity_log.dart';
import 'package:intl/intl.dart';

class ActivityFeedWidget extends StatelessWidget {
  final List<ActivityLog> activities;

  const ActivityFeedWidget({super.key, required this.activities});

  @override
  Widget build(BuildContext context) {
    if (activities.isEmpty) {
      return const Center(child: Text('No recent activity.'));
    }
    return ListView.builder(
      itemCount: activities.length,
      itemBuilder: (context, index) {
        final activity = activities[index];
        return ListTile(
          leading: const Icon(Icons.history),
          title: Text(activity.summary),
          subtitle: Text(
            '${activity.actorId} • ${DateFormat.yMMMd().add_jm().format(activity.createdAt)}',
          ),
        );
      },
    );
  }
}
