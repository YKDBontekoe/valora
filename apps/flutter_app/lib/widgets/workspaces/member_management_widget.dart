import 'package:flutter/material.dart';
import '../../models/workspace.dart';
import 'share_workspace_dialog.dart';
import '../valora_widgets.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';

class MemberManagementWidget extends StatelessWidget {
  final List<WorkspaceMember> members;
  final bool canInvite;

  const MemberManagementWidget({super.key, required this.members, this.canInvite = false});

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (canInvite)
          Padding(
            padding: const EdgeInsets.all(ValoraSpacing.md),
            child: ValoraButton(
              onPressed: () => showDialog(
                context: context,
                builder: (_) => const ShareWorkspaceDialog(),
              ),
              label: 'Invite Member',
              variant: ValoraButtonVariant.primary,
              icon: Icons.person_add_rounded,
            ),
          ),
        if (members.isEmpty)
          Expanded(
            child: Center(
              child: ValoraEmptyState(
                icon: Icons.people_alt_rounded,
                title: 'No members yet',
                subtitle: 'Invite people to collaborate in this workspace.',
              ),
            ),
          )
        else
          Expanded(
            child: ListView.separated(
              padding: const EdgeInsets.symmetric(horizontal: ValoraSpacing.md, vertical: ValoraSpacing.sm),
              itemCount: members.length,
              separatorBuilder: (context, index) => const SizedBox(height: ValoraSpacing.sm),
              itemBuilder: (context, index) {
                final member = members[index];
                return ValoraCard(
                  padding: const EdgeInsets.all(ValoraSpacing.md),
                  child: Row(
                    children: [
                      ValoraAvatar(
                        initials: member.email?.isNotEmpty == true ? member.email![0].toUpperCase() : '?',
                        size: ValoraAvatarSize.medium,
                      ),
                      const SizedBox(width: ValoraSpacing.md),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              member.email ?? 'Unknown User',
                              style: ValoraTypography.titleMedium.copyWith(
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                            const SizedBox(height: 2),
                            Text(
                              member.role.name.toUpperCase(),
                              style: ValoraTypography.bodyMedium.copyWith(
                                color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
                              ),
                            ),
                          ],
                        ),
                      ),
                      if (member.isPending)
                        ValoraBadge(
                          label: 'Pending',
                          color: ValoraColors.warning,
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
}
