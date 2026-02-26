import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../models/workspace.dart';
import '../../providers/auth_provider.dart';
import '../../providers/workspace_provider.dart';
import '../valora_widgets.dart';
import 'share_workspace_dialog.dart';

class MemberManagementWidget extends StatelessWidget {
  final List<WorkspaceMember> members;
  final bool canInvite;

  const MemberManagementWidget({super.key, required this.members, this.canInvite = false});

  void _showRemoveConfirmation(BuildContext context, WorkspaceMember member) {
    final isPending = member.isPending;
    final title = isPending ? 'Cancel Invite?' : 'Remove Member?';
    final content = isPending
        ? 'Are you sure you want to cancel the invitation for ${member.email}?'
        : 'Are you sure you want to remove ${member.email ?? 'this user'} from the workspace?';

    showDialog(
      context: context,
      builder: (ctx) => ValoraDialog(
        title: title,
        actions: [
          ValoraButton(
            label: 'No',
            variant: ValoraButtonVariant.ghost,
            onPressed: () => Navigator.pop(ctx),
          ),
          ValoraButton(
            label: 'Yes, Remove',
            variant: ValoraButtonVariant.primary,
            onPressed: () {
              context.read<WorkspaceProvider>().removeMember(member.id);
              Navigator.pop(ctx);
            },
          ),
        ],
        child: Text(content),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final currentUserEmail = context.select<AuthProvider, String?>((p) => p.email);
    // Find current user's role to determine permissions
    final currentUserMember = members.where((m) => m.email == currentUserEmail).firstOrNull;
    final isOwner = currentUserMember?.role == WorkspaceRole.owner;

    return Column(
      children: [
        if (canInvite)
          Padding(
            padding: const EdgeInsets.all(ValoraSpacing.md),
            child: ValoraButton(
              onPressed: () => showDialog(
                context: context,
                builder: (_) => const ShareWorkspaceDialog(),
              ),
              icon: Icons.person_add_rounded,
              label: 'Invite Member',
              variant: ValoraButtonVariant.outline,
              isFullWidth: true,
            ),
          ),
        Expanded(
          child: ListView.separated(
            padding: const EdgeInsets.symmetric(horizontal: ValoraSpacing.md),
            itemCount: members.length,
            separatorBuilder: (_, _) => const SizedBox(height: ValoraSpacing.sm),
            itemBuilder: (context, index) {
              final member = members[index];
              final isMe = member.email == currentUserEmail;

              // Only owners can remove others. Cannot remove self via this list (use Leave).
              // Can remove pending invites.
              final canRemove = isOwner && !isMe;

              return ValoraCard(
                padding: const EdgeInsets.all(ValoraSpacing.sm),
                child: ListTile(
                  contentPadding: EdgeInsets.zero,
                  leading: ValoraAvatar(
                    initials: member.email?.isNotEmpty == true ? member.email![0] : '?',
                    size: ValoraAvatarSize.medium,
                  ),
                  title: Text(
                    member.email ?? 'Unknown User',
                    style: ValoraTypography.bodyMedium.copyWith(fontWeight: FontWeight.w600),
                  ),
                  subtitle: Text(
                    member.role.name.toUpperCase(),
                    style: ValoraTypography.labelSmall.copyWith(color: ValoraColors.neutral500),
                  ),
                  trailing: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      if (member.isPending)
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                          decoration: BoxDecoration(
                            color: ValoraColors.warning.withValues(alpha: 0.1),
                            borderRadius: BorderRadius.circular(12),
                            border: Border.all(color: ValoraColors.warning.withValues(alpha: 0.2)),
                          ),
                          child: Text(
                            'Pending',
                            style: ValoraTypography.labelSmall.copyWith(
                              color: ValoraColors.warning,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                        ),
                      if (canRemove) ...[
                         const SizedBox(width: 8),
                         IconButton(
                           icon: Icon(
                             member.isPending ? Icons.close_rounded : Icons.person_remove_rounded,
                             color: ValoraColors.error,
                             size: 20,
                           ),
                           tooltip: member.isPending ? 'Cancel Invite' : 'Remove Member',
                           onPressed: () => _showRemoveConfirmation(context, member),
                         ),
                      ],
                    ],
                  ),
                ),
              );
            },
          ),
        ),
      ],
    );
  }
}
