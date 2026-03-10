import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:provider/provider.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_typography.dart';
import '../../core/theme/valora_spacing.dart';
import '../../models/workspace.dart';
import '../../providers/workspace_provider.dart';
import '../../widgets/valora_widgets.dart';
import '../../screens/workspace_detail_screen.dart';

class WorkspaceListItem extends StatelessWidget {
  final Workspace workspace;
  final int index;

  const WorkspaceListItem({
    super.key,
    required this.workspace,
    this.index = 0,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    return ValoraCard(
      onTap: () {
        Navigator.push(
          context,
          MaterialPageRoute(
            builder: (_) => ChangeNotifierProvider.value(
              value: context.read<WorkspaceProvider>(),
              child: WorkspaceDetailScreen(workspaceId: workspace.id),
            ),
          ),
        );
      },
      padding: const EdgeInsets.all(ValoraSpacing.md),
      child: Row(
        children: [
          ValoraAvatar(
            initials:
                workspace.name.isNotEmpty ? workspace.name[0].toUpperCase() : 'W',
            size: ValoraAvatarSize.medium,
          ),
          const SizedBox(width: ValoraSpacing.md),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  workspace.name,
                  style: ValoraTypography.titleMedium.copyWith(
                    fontWeight: FontWeight.w600,
                  ),
                ),
                const SizedBox(height: 4),
                Row(
                  children: [
                    Icon(Icons.people_alt_rounded,
                        size: 14,
                        color: isDark
                            ? ValoraColors.neutral400
                            : ValoraColors.neutral500),
                    const SizedBox(width: 4),
                    Text(
                      '${workspace.memberCount} members',
                      style: ValoraTypography.labelSmall.copyWith(
                        color: isDark
                            ? ValoraColors.neutral400
                            : ValoraColors.neutral500,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Icon(Icons.bookmark_rounded,
                        size: 14,
                        color: isDark
                            ? ValoraColors.neutral400
                            : ValoraColors.neutral500),
                    const SizedBox(width: 4),
                    Text(
                      '${workspace.savedListingCount} saved',
                      style: ValoraTypography.labelSmall.copyWith(
                        color: isDark
                            ? ValoraColors.neutral400
                            : ValoraColors.neutral500,
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
          PopupMenuButton<String>(
            icon: Icon(Icons.more_vert_rounded,
                color: isDark ? ValoraColors.neutral500 : ValoraColors.neutral400),
            onSelected: (value) {
              if (value == 'delete') {
                _showDeleteConfirmation(context, workspace);
              }
            },
            itemBuilder: (context) => [
              const PopupMenuItem(
                value: 'delete',
                child: Row(
                  children: [
                    Icon(Icons.delete_outline_rounded,
                        color: ValoraColors.error, size: 20),
                    SizedBox(width: 8),
                    Text('Delete', style: TextStyle(color: ValoraColors.error)),
                  ],
                ),
              ),
            ],
          ),
        ],
      ),
    )
        .animate(delay: (50 * index).ms)
        .fadeIn(duration: 300.ms, curve: Curves.easeOut)
        .slideY(begin: 0.1, end: 0, duration: 300.ms, curve: Curves.easeOut);
  }

  void _showDeleteConfirmation(BuildContext context, Workspace workspace) {
    showDialog(
      context: context,
      builder: (ctx) => ValoraDialog(
        title: 'Delete Workspace?',
        actions: [
          ValoraButton(
            label: 'Cancel',
            variant: ValoraButtonVariant.ghost,
            onPressed: () => Navigator.pop(ctx),
          ),
          Consumer<WorkspaceProvider>(
            builder: (context, provider, _) {
              return ValoraButton(
                label: 'Delete',
                variant: ValoraButtonVariant.primary,
                isLoading: provider.isDeletingWorkspace,
                onPressed: () async {
                  try {
                    await provider.deleteWorkspace(workspace.id);
                    if (context.mounted) {
                      Navigator.pop(ctx);
                      ScaffoldMessenger.of(context).showSnackBar(
                        const SnackBar(
                          content: Text('Workspace deleted successfully'),
                          backgroundColor: ValoraColors.success,
                        ),
                      );
                    }
                  } catch (e) {
                    if (context.mounted) {
                      ScaffoldMessenger.of(context).showSnackBar(
                        SnackBar(
                          content: Text('Failed to delete workspace: $e'),
                          backgroundColor: ValoraColors.error,
                        ),
                      );
                    }
                  }
                },
              );
            },
          ),
        ],
        child: Text(
            'Are you sure you want to delete "${workspace.name}"? This action cannot be undone and all data including saved listings and comments will be lost.'),
      ),
    );
  }
}

class WorkspaceListSkeleton extends StatelessWidget {
  const WorkspaceListSkeleton({super.key});

  @override
  Widget build(BuildContext context) {
    return ValoraCard(
      padding: const EdgeInsets.all(ValoraSpacing.md),
      child: Row(
        children: [
          const ValoraShimmer(width: 48, height: 48, borderRadius: 14),
          const SizedBox(width: ValoraSpacing.md),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const ValoraShimmer(width: 140, height: 20, borderRadius: 4),
                const SizedBox(height: 8),
                Row(
                  children: [
                    const ValoraShimmer(width: 80, height: 14, borderRadius: 4),
                    const SizedBox(width: 12),
                    const ValoraShimmer(width: 60, height: 14, borderRadius: 4),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
