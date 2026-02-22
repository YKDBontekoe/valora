import 'package:flutter/material.dart';
import '../../models/workspace.dart';
import '../../providers/workspace_provider.dart';
import 'package:provider/provider.dart';

class ShareWorkspaceDialog extends StatefulWidget {
  const ShareWorkspaceDialog({super.key});

  @override
  State<ShareWorkspaceDialog> createState() => _ShareWorkspaceDialogState();
}

class _ShareWorkspaceDialogState extends State<ShareWorkspaceDialog> {
  final _emailController = TextEditingController();
  WorkspaceRole _role = WorkspaceRole.viewer;

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text('Invite Member'),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          TextField(
            controller: _emailController,
            decoration: const InputDecoration(labelText: 'Email Address'),
          ),
          const SizedBox(height: 16),
          DropdownButton<WorkspaceRole>(
            value: _role,
            isExpanded: true,
            items: WorkspaceRole.values.map((r) {
              return DropdownMenuItem(
                value: r,
                child: Text(r.name.toUpperCase()),
              );
            }).toList(),
            onChanged: (val) {
              if (val != null) setState(() => _role = val);
            },
          ),
        ],
      ),
      actions: [
        TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancel')),
        ElevatedButton(
          onPressed: () async {
            try {
              await context.read<WorkspaceProvider>().inviteMember(_emailController.text, _role);
              if (context.mounted) Navigator.pop(context);
            } catch (e) {
              if (context.mounted) {
                ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Failed: $e')));
              }
            }
          },
          child: const Text('Invite'),
        ),
      ],
    );
  }
}
