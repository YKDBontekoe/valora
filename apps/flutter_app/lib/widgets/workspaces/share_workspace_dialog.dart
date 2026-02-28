import 'package:flutter/material.dart';
import '../../models/workspace.dart';
import '../../providers/workspace_provider.dart';
import 'package:provider/provider.dart';
import '../valora_widgets.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';

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
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return ValoraDialog(
      title: 'Invite Member',
      actions: [
        ValoraButton(
          label: 'Cancel',
          variant: ValoraButtonVariant.ghost,
          onPressed: () => Navigator.pop(context),
        ),
        ValoraButton(
          label: 'Invite',
          variant: ValoraButtonVariant.primary,
          isLoading: _isLoading,
          onPressed: _submit,
        ),
      ],
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          ValoraTextField(
            controller: _emailController,
            label: 'Email Address',
            hint: 'e.g. hello@example.com',
            prefixIcon: const Icon(Icons.email_outlined, size: 20),
          ),
          const SizedBox(height: ValoraSpacing.md),
          Text(
            'Role',
            style: ValoraTypography.labelMedium.copyWith(
              color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
            ),
          ),
          const SizedBox(height: 8),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: ValoraSpacing.sm),
            decoration: BoxDecoration(
              color: isDark ? ValoraColors.neutral800 : ValoraColors.neutral50,
              border: Border.all(
                color: isDark ? ValoraColors.neutral700 : ValoraColors.neutral200,
              ),
              borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
            ),
            child: DropdownButtonHideUnderline(
              child: DropdownButton<WorkspaceRole>(
                value: _role,
                isExpanded: true,
                dropdownColor: isDark ? ValoraColors.neutral800 : ValoraColors.neutral50,
                style: ValoraTypography.bodyMedium.copyWith(
                  color: colorScheme.onSurface,
                ),
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
            ),
          ),
        ],
      ),
    );
  }

  Future<void> _submit() async {
    if (_isLoading) return;
    final email = _emailController.text.trim();
    if (email.isEmpty) return;

    setState(() => _isLoading = true);
    final messenger = ScaffoldMessenger.of(context);
    try {
      await context.read<WorkspaceProvider>().inviteMember(email, _role);
      if (mounted) {
        Navigator.pop(context);
        messenger.showSnackBar(
          const SnackBar(
            content: Text('Member invited successfully'),
            backgroundColor: ValoraColors.success,
          ),
        );
      }
    } catch (e) {
      if (mounted) {
        messenger.showSnackBar(
          SnackBar(
            content: Text('Failed: $e'),
            backgroundColor: ValoraColors.error,
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }
}
