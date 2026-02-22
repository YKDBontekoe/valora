import 'package:flutter/material.dart';
import '../../models/workspace.dart';
import 'share_workspace_dialog.dart';

class MemberManagementWidget extends StatelessWidget {
  final List<WorkspaceMember> members;
  final bool canInvite;

  const MemberManagementWidget({super.key, required this.members, this.canInvite = false});

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        if (canInvite)
          Padding(
            padding: const EdgeInsets.all(8.0),
            child: ElevatedButton.icon(
              onPressed: () => showDialog(
                context: context,
                builder: (_) => const ShareWorkspaceDialog(),
              ),
              icon: const Icon(Icons.person_add),
              label: const Text('Invite Member'),
            ),
          ),
        Expanded(
          child: ListView.builder(
            itemCount: members.length,
            itemBuilder: (context, index) {
              final member = members[index];
              return ListTile(
                leading: CircleAvatar(child: Text(member.email?.isNotEmpty == true ? member.email![0].toUpperCase() : '?')),
                title: Text(member.email ?? 'Unknown User'),
                subtitle: Text(member.role.name.toUpperCase()),
                trailing: member.isPending ? const Chip(label: Text('Pending')) : null,
              );
            },
          ),
        ),
      ],
    );
  }
}
