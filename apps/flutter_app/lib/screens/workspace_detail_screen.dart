import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_typography.dart';
import '../core/theme/valora_spacing.dart';
import '../providers/workspace_provider.dart';
import '../widgets/valora_widgets.dart';
import '../widgets/workspaces/activity_feed_widget.dart';
import '../models/activity_log.dart';
import '../models/workspace.dart';
import '../models/saved_property.dart';
import '../widgets/workspaces/member_management_widget.dart';

class WorkspaceDetailScreen extends StatefulWidget {
  final String workspaceId;

  const WorkspaceDetailScreen({super.key, required this.workspaceId});

  @override
  State<WorkspaceDetailScreen> createState() => _WorkspaceDetailScreenState();
}

class _WorkspaceDetailScreenState extends State<WorkspaceDetailScreen>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 3, vsync: this);
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<WorkspaceProvider>().selectWorkspace(widget.workspaceId);
    });
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final colorScheme = Theme.of(context).colorScheme;

    return Scaffold(
      backgroundColor: colorScheme.surface,
      appBar: AppBar(
        backgroundColor: colorScheme.surface.withValues(alpha: 0.95),
        surfaceTintColor: Colors.transparent,
        title: Consumer<WorkspaceProvider>(
          builder: (_, p, child) => Text(
            p.selectedWorkspace?.name ?? 'Workspace',
            style: ValoraTypography.titleLarge.copyWith(
              fontWeight: FontWeight.bold,
              color: colorScheme.onSurface,
            ),
          ),
        ),
        bottom: TabBar(
          controller: _tabController,
          labelColor: ValoraColors.primary,
          unselectedLabelColor:
              isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
          indicatorColor: ValoraColors.primary,
          indicatorSize: TabBarIndicatorSize.label,
          labelStyle: ValoraTypography.labelLarge.copyWith(
            fontWeight: FontWeight.bold,
          ),
          unselectedLabelStyle: ValoraTypography.labelLarge,
          tabs: const [
            Tab(
              icon: Icon(Icons.bookmark_rounded, size: 20),
              text: 'Saved',
            ),
            Tab(
              icon: Icon(Icons.people_alt_rounded, size: 20),
              text: 'Members',
            ),
            Tab(
              icon: Icon(Icons.history_rounded, size: 20),
              text: 'Activity',
            ),
          ],
        ),
      ),
      body: Selector<WorkspaceProvider, ({bool isLoading, String? error})>(
        selector: (_, p) => (isLoading: p.isWorkspaceDetailLoading, error: p.error),
        builder: (context, data, child) {
          if (data.isLoading) {
            return const Center(
              child: ValoraLoadingIndicator(message: 'Loading workspace...'),
            );
          }
          if (data.error != null) {
            return Center(
              child: ValoraEmptyState(
                icon: Icons.error_outline_rounded,
                title: 'Something went wrong',
                subtitle: 'Could not load workspace details.',
                actionLabel: 'Retry',
                onAction: () =>
                    context.read<WorkspaceProvider>().selectWorkspace(widget.workspaceId),
              ),
            );
          }

          return TabBarView(
            controller: _tabController,
            children: const [
              _SavedPropertiesTab(),
              _MembersTab(),
              _ActivityTab(),
            ],
          );
        },
      ),
    );
  }
}

class _SavedPropertiesTab extends StatelessWidget {
  const _SavedPropertiesTab();

  @override
  Widget build(BuildContext context) {
    return Selector<WorkspaceProvider, List<SavedProperty>>(
      selector: (_, p) => p.savedProperties,
      builder: (context, savedProperties, child) {
        final isDark = Theme.of(context).brightness == Brightness.dark;

        if (savedProperties.isEmpty) {
          return Center(
            child: ValoraEmptyState(
              icon: Icons.bookmark_add_rounded,
              title: 'No saved properties',
              subtitle: 'Reports you save to this workspace will appear here.',
            ),
          );
        }
        return ListView.separated(
          padding: const EdgeInsets.all(ValoraSpacing.md),
          itemCount: savedProperties.length,
          separatorBuilder: (_, _) => const SizedBox(height: ValoraSpacing.sm),
          itemBuilder: (context, index) {
            final saved = savedProperties[index];
            final property = saved.property;
            return ValoraCard(
              onTap: () {
                // Navigate to property detail (Context Report view)
              },
              padding: const EdgeInsets.all(ValoraSpacing.md),
              child: Row(
                children: [
                  // Icon instead of Funda image
                  Container(
                    width: 48,
                    height: 48,
                    decoration: BoxDecoration(
                      borderRadius: BorderRadius.circular(12),
                      color: ValoraColors.primary.withValues(alpha: 0.1),
                    ),
                    child: const Center(
                      child: Icon(Icons.location_on_rounded, color: ValoraColors.primary),
                    ),
                  ),
                  const SizedBox(width: ValoraSpacing.md),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          property?.address ?? 'Unknown Address',
                          style: ValoraTypography.titleSmall.copyWith(
                            fontWeight: FontWeight.w600,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                        const SizedBox(height: 4),
                        Row(
                          children: [
                            if (property?.compositeScore != null) ...[
                              Container(
                                padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                                decoration: BoxDecoration(
                                  color: ValoraColors.primary.withValues(alpha: 0.1),
                                  borderRadius: BorderRadius.circular(4),
                                ),
                                child: Text(
                                  '${property!.compositeScore!.toStringAsFixed(0)}',
                                  style: ValoraTypography.labelSmall.copyWith(
                                    color: ValoraColors.primary,
                                    fontWeight: FontWeight.bold,
                                  ),
                                ),
                              ),
                              const SizedBox(width: 8),
                            ],
                            Icon(Icons.chat_bubble_outline_rounded,
                                size: 14,
                                color: isDark
                                    ? ValoraColors.neutral500
                                    : ValoraColors.neutral400),
                            const SizedBox(width: 4),
                            Text(
                              '${saved.commentCount}',
                              style: ValoraTypography.labelSmall.copyWith(
                                color: isDark
                                    ? ValoraColors.neutral500
                                    : ValoraColors.neutral400,
                              ),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                  Icon(Icons.chevron_right_rounded,
                      color: isDark
                          ? ValoraColors.neutral500
                          : ValoraColors.neutral400),
                ],
              ),
            );
          },
        );
      },
    );
  }
}

class _MembersTab extends StatelessWidget {
  const _MembersTab();

  @override
  Widget build(BuildContext context) {
    return Selector<WorkspaceProvider, List<WorkspaceMember>>(
      selector: (_, p) => p.members,
      builder: (context, members, child) {
        return MemberManagementWidget(
          members: members,
          canInvite: true,
        );
      },
    );
  }
}

class _ActivityTab extends StatelessWidget {
  const _ActivityTab();

  @override
  Widget build(BuildContext context) {
    return Selector<WorkspaceProvider, List<ActivityLog>>(
      selector: (_, p) => p.activityLogs,
      builder: (context, logs, child) {
        return ActivityFeedWidget(activities: logs);
      },
    );
  }
}
