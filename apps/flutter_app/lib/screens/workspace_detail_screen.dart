import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
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
          if (provider.isWorkspaceDetailLoading) {
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
      separatorBuilder: (_, _) => const SizedBox(height: ValoraSpacing.md),
      itemBuilder: (context, index) {
        final saved = provider.savedListings[index];
        final listing = saved.listing;

        if (listing == null) {
          return const SizedBox.shrink();
        }

        return ValoraListingCard(
          listing: listing,
          commentCount: saved.commentCount,
          notes: saved.notes,
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
        );
      },
    );
  }
}
