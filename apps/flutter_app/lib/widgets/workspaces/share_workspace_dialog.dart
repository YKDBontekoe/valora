import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../models/workspace.dart';
import '../../providers/workspace_provider.dart';
import '../valora_widgets.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../core/theme/valora_colors.dart';

class ShareWorkspaceDialog extends StatefulWidget {
  const ShareWorkspaceDialog({super.key});

  @override
  State<ShareWorkspaceDialog> createState() => _ShareWorkspaceDialogState();
}

class _ShareWorkspaceDialogState extends State<ShareWorkspaceDialog> {
  final _emailController = TextEditingController();
  WorkspaceRole _role = WorkspaceRole.viewer;
  bool _isLoading = false;

  @override
  void dispose() {
    _emailController.dispose();
    super.dispose();
  }

  Future<void> _handleInvite() async {
    if (_emailController.text.isEmpty) return;

    setState(() => _isLoading = true);
    try {
      await context.read<WorkspaceProvider>().inviteMember(_emailController.text, _role);
      if (mounted) Navigator.pop(context);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Failed: $e'),
            backgroundColor: ValoraColors.error,
            behavior: SnackBarBehavior.floating,
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return ValoraDialog(
      title: 'Invite Member',
      actions: [
        ValoraButton(
          label: 'Cancel',
          variant: ValoraButtonVariant.ghost,
          onPressed: () => Navigator.pop(context),
        ),
        ValoraButton(
          label: 'Send Invite',
          onPressed: _handleInvite,
          isLoading: _isLoading,
        ),
      ],
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          ValoraTextField(
            controller: _emailController,
            label: 'Email Address',
            hint: 'colleague@example.com',
            keyboardType: TextInputType.emailAddress,
            prefixIcon: const Icon(Icons.email_outlined),
          ),
          const SizedBox(height: ValoraSpacing.lg),
          Text(
            'Role',
            style: ValoraTypography.labelMedium.copyWith(
              fontWeight: FontWeight.bold,
            ),
          ),
          const SizedBox(height: ValoraSpacing.sm),
          Wrap(
            spacing: ValoraSpacing.sm,
            children: WorkspaceRole.values.map((role) {
              final isSelected = _role == role;
              return ValoraChip(
                label: role.name.toUpperCase(),
                isSelected: isSelected,
                onSelected: (_) => setState(() => _role = role),
              );
            }).toList(),
          ),
        ],
      ),
    );
  }
}
