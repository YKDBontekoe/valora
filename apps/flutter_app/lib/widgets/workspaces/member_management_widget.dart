import 'package:flutter/material.dart';
import '../../models/workspace.dart';
import 'share_workspace_dialog.dart';
import '../valora_widgets.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_typography.dart';

class MemberManagementWidget extends StatelessWidget {
  final List<WorkspaceMember> members;
  final bool canInvite;

  const MemberManagementWidget({super.key, required this.members, this.canInvite = false});

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    if (members.isEmpty) {
      return Center(
        child: ValoraEmptyState(
          icon: Icons.people_outline_rounded,
          title: 'No members yet',
          subtitle: 'Invite people to collaborate in this workspace.',
          actionLabel: canInvite ? 'Invite Member' : null,
          onAction: canInvite ? () => _showInviteDialog(context) : null,
        ),
      );
    }

    return Column(
      children: [
        if (canInvite)
          Padding(
            padding: const EdgeInsets.all(ValoraSpacing.md),
            child: ValoraButton(
              label: 'Invite Member',
              icon: Icons.person_add_rounded,
              isFullWidth: true,
              variant: ValoraButtonVariant.secondary,
              onPressed: () => _showInviteDialog(context),
            ),
          ),
        Expanded(
          child: ListView.separated(
            padding: const EdgeInsets.symmetric(horizontal: ValoraSpacing.md),
            itemCount: members.length,
            separatorBuilder: (context, index) => const SizedBox(height: ValoraSpacing.sm),
            itemBuilder: (context, index) {
              final member = members[index];
              return ValoraCard(
                padding: const EdgeInsets.all(ValoraSpacing.md),
                child: Row(
                  children: [
                    ValoraAvatar(
                      initials: member.email?.isNotEmpty == true ? member.email![0] : '?',
                      size: ValoraAvatarSize.medium,
                    ),
                    const SizedBox(width: ValoraSpacing.md),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            member.email ?? 'Unknown User',
                            style: ValoraTypography.bodyLarge.copyWith(
                              fontWeight: FontWeight.w600,
                              color: isDark ? ValoraColors.neutral100 : ValoraColors.neutral900,
                            ),
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                          ),
                          const SizedBox(height: 4),
                          Row(
                            children: [
                              ValoraBadge(
                                label: member.role.name.toUpperCase(),
                                size: ValoraBadgeSize.small,
                                color: ValoraColors.primary,
                              ),
                              if (member.isPending) ...[
                                const SizedBox(width: ValoraSpacing.xs),
                                const ValoraBadge(
                                  label: 'PENDING',
                                  size: ValoraBadgeSize.small,
                                  color: ValoraColors.warning,
                                ),
                              ],
                            ],
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              );
            },
          ),
        ),
      ],
    );
  }

  void _showInviteDialog(BuildContext context) {
    showDialog(
      context: context,
      builder: (_) => const ShareWorkspaceDialog(),
    );
  }
}
