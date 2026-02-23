import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_typography.dart';
import '../core/theme/valora_spacing.dart';
import '../providers/workspace_provider.dart';
import '../widgets/valora_widgets.dart';
import '../widgets/workspaces/activity_feed_widget.dart';
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
      body: Consumer<WorkspaceProvider>(
        builder: (context, provider, child) {
          if (provider.isLoading) {
            return const Center(
              child: ValoraLoadingIndicator(message: 'Loading workspace...'),
            );
          }
          if (provider.error != null) {
            return Center(
              child: ValoraEmptyState(
                icon: Icons.error_outline_rounded,
                title: 'Something went wrong',
                subtitle: 'Could not load workspace details.',
                actionLabel: 'Retry',
                onAction: () =>
                    provider.selectWorkspace(widget.workspaceId),
              ),
            );
          }

          return TabBarView(
            controller: _tabController,
            children: [
              _buildSavedListings(context, provider),
              MemberManagementWidget(
                members: provider.members,
                canInvite: true,
              ),
              ActivityFeedWidget(activities: provider.activityLogs),
            ],
          );
        },
      ),
    );
  }

  Widget _buildSavedListings(
      BuildContext context, WorkspaceProvider provider) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    if (provider.savedListings.isEmpty) {
      return Center(
        child: ValoraEmptyState(
          icon: Icons.bookmark_add_rounded,
          title: 'No saved listings',
          subtitle:
              'Properties you save to this workspace will appear here.',
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
        return ValoraListItem(
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
          title: listing?.address ?? 'Unknown Address',
          leading: Container(
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
                    placeholder: (context, url) =>
                        const ValoraShimmer(width: 64, height: 64),
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
          subtitleWidget: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              if (listing?.city != null && listing!.city!.isNotEmpty) ...[
                Text(
                  listing.city!,
                  style: ValoraTypography.bodySmall.copyWith(
                    color: isDark
                        ? ValoraColors.neutral400
                        : ValoraColors.neutral500,
                  ),
                ),
                const SizedBox(height: 6),
              ],
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
                  if (saved.notes != null && saved.notes!.isNotEmpty) ...[
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
        )
            .animate()
            .fadeIn(duration: 400.ms, delay: (50 * index).ms)
            .slideX(begin: 0.1, duration: 400.ms, curve: Curves.easeOut);
      },
    );
  }
}
