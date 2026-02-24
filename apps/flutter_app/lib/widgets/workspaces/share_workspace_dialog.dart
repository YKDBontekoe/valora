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

  bool _isValidEmail(String email) {
    return RegExp(r"^[a-zA-Z0-9.a-zA-Z0-9.!#$%&'*+-/=?^_`{|}~]+@[a-zA-Z0-9]+\.[a-zA-Z]+").hasMatch(email);
  }

  Future<void> _handleInvite() async {
    final email = _emailController.text.trim();
    if (email.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Please enter an email address'),
          backgroundColor: ValoraColors.warning,
          behavior: SnackBarBehavior.floating,
        ),
      );
      return;
    }

    if (!_isValidEmail(email)) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Please enter a valid email address'),
          backgroundColor: ValoraColors.warning,
          behavior: SnackBarBehavior.floating,
        ),
      );
      return;
    }

    setState(() => _isLoading = true);
    try {
      await context.read<WorkspaceProvider>().inviteMember(email, _role);
      if (mounted) Navigator.pop(context);
    } catch (e) {
      if (mounted) {
        // Log the actual error internally if needed: print('Invite failed: $e');
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Failed to send invite. Please try again.'),
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
