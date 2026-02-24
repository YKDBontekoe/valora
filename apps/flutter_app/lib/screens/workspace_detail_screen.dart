import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:cached_network_image/cached_network_image.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_typography.dart';
import '../core/theme/valora_spacing.dart';
import '../providers/workspace_provider.dart';
import '../widgets/valora_widgets.dart';
import '../widgets/workspaces/activity_feed_widget.dart';
import '../models/activity_log.dart';
import '../models/workspace.dart';
import '../widgets/workspaces/member_management_widget.dart';
import 'saved_listing_detail_screen.dart';

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
              _SavedListingsTab(),
              _MembersTab(),
              _ActivityTab(),
            ],
          );
        },
      ),
    );
  }
}

class _SavedListingsTab extends StatelessWidget {
  const _SavedListingsTab();

  @override
  Widget build(BuildContext context) {
    return Consumer<WorkspaceProvider>(
      builder: (context, provider, child) {
        final isDark = Theme.of(context).brightness == Brightness.dark;

        if (provider.savedListings.isEmpty) {
          return Center(
            child: ValoraEmptyState(
              icon: Icons.bookmark_add_rounded,
              title: 'No saved listings',
              subtitle: 'Properties you save to this workspace will appear here.',
            ),
          );
        }
        return ListView.separated(
          padding: const EdgeInsets.all(ValoraSpacing.md),
          itemCount: provider.savedListings.length,
          separatorBuilder: (_, _) => const SizedBox(height: ValoraSpacing.sm),
          itemBuilder: (context, index) {
            final saved = provider.savedListings[index];
            final listing = saved.listing;
            return ValoraCard(
              onTap: () {
                Navigator.push(
                  context,
                  MaterialPageRoute(
                    builder: (_) => ChangeNotifierProvider.value(
                      value: context.read<WorkspaceProvider>(),
                      child: SavedListingDetailScreen(savedListing: saved),
                    ),
                  ),
                );
              },
              padding: const EdgeInsets.all(ValoraSpacing.md),
              child: Row(
                children: [
                  // Thumbnail
                  Container(
                    width: 64,
                    height: 64,
                    decoration: BoxDecoration(
                      borderRadius: BorderRadius.circular(12),
                      color: ValoraColors.primary.withValues(alpha: 0.08),
                    ),
                    clipBehavior: Clip.antiAlias,
                    child: listing?.imageUrl != null
                        ? CachedNetworkImage(
                            imageUrl: listing!.imageUrl!,
                            fit: BoxFit.cover,
                            placeholder: (context, url) => const ValoraShimmer(
                              width: double.infinity,
                              height: double.infinity,
                            ),
                            errorWidget: (context, url, error) => const Center(
                              child: Icon(Icons.home_rounded,
                                  color: ValoraColors.primary, size: 28),
                            ),
                          )
                        : const Center(
                            child: Icon(Icons.home_rounded,
                                color: ValoraColors.primary, size: 28),
                          ),
                  ),
                  const SizedBox(width: ValoraSpacing.md),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          listing?.address ?? 'Unknown Address',
                          style: ValoraTypography.titleSmall.copyWith(
                            fontWeight: FontWeight.w600,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                        if (listing?.city != null &&
                            listing!.city!.isNotEmpty) ...[
                          const SizedBox(height: 2),
                          Text(
                            listing.city!,
                            style: ValoraTypography.bodySmall.copyWith(
                              color: isDark
                                  ? ValoraColors.neutral400
                                  : ValoraColors.neutral500,
                            ),
                          ),
                        ],
                        const SizedBox(height: 6),
                        Row(
                          children: [
                            Icon(Icons.chat_bubble_outline_rounded,
                                size: 14,
                                color: isDark
                                    ? ValoraColors.neutral500
                                    : ValoraColors.neutral400),
                            const SizedBox(width: 4),
                            Text(
                              '${saved.commentCount} comments',
                              style: ValoraTypography.labelSmall.copyWith(
                                color: isDark
                                    ? ValoraColors.neutral500
                                    : ValoraColors.neutral400,
                              ),
                            ),
                            if (saved.notes != null &&
                                saved.notes!.isNotEmpty) ...[
                              const SizedBox(width: 12),
                              Icon(Icons.note_rounded,
                                  size: 14,
                                  color: isDark
                                      ? ValoraColors.neutral500
                                      : ValoraColors.neutral400),
                              const SizedBox(width: 4),
                              Flexible(
                                child: Text(
                                  saved.notes!,
                                  style: ValoraTypography.labelSmall.copyWith(
                                    color: isDark
                                        ? ValoraColors.neutral500
                                        : ValoraColors.neutral400,
                                  ),
                                  maxLines: 1,
                                  overflow: TextOverflow.ellipsis,
                                ),
                              ),
                            ],
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
