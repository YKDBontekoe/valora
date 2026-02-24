import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:cached_network_image/cached_network_image.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_typography.dart';
import '../core/theme/valora_spacing.dart';
import '../providers/workspace_provider.dart';
import '../widgets/valora_widgets.dart';
import '../widgets/workspaces/activity_feed_widget.dart';
import '../widgets/workspaces/member_management_widget.dart';
import 'saved_listing_detail_screen.dart';

enum WorkspaceAction { edit, delete }

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
        actions: [
          Consumer<WorkspaceProvider>(
            builder: (context, provider, _) {
              if (provider.selectedWorkspace == null) return const SizedBox.shrink();
              return PopupMenuButton<WorkspaceAction>(
                onSelected: (action) {
                  if (action == WorkspaceAction.edit) {
                    _showEditDialog(context, provider);
                  } else if (action == WorkspaceAction.delete) {
                    _showDeleteConfirmation(context, provider);
                  }
                },
                itemBuilder: (context) => [
                  const PopupMenuItem(
                    value: WorkspaceAction.edit,
                    child: Row(
                      children: [
                        Icon(Icons.edit_rounded, size: 20),
                        SizedBox(width: 12),
                        Text('Edit Workspace'),
                      ],
                    ),
                  ),
                  const PopupMenuItem(
                    value: WorkspaceAction.delete,
                    child: Row(
                      children: [
                        Icon(Icons.delete_rounded, size: 20, color: ValoraColors.error),
                        SizedBox(width: 12),
                        Text('Delete Workspace', style: TextStyle(color: ValoraColors.error)),
                      ],
                    ),
                  ),
                ],
              );
            },
          ),
        ],
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

  void _showEditDialog(BuildContext context, WorkspaceProvider provider) {
    final workspace = provider.selectedWorkspace;
    if (workspace == null) return;

    final nameController = TextEditingController(text: workspace.name);
    final descController = TextEditingController(text: workspace.description);

    showDialog(
      context: context,
      builder: (ctx) => ValoraDialog(
        title: 'Edit Workspace',
        actions: [
          ValoraButton(
            label: 'Cancel',
            variant: ValoraButtonVariant.ghost,
            onPressed: () => Navigator.pop(ctx),
          ),
          ValoraButton(
            label: 'Save',
            variant: ValoraButtonVariant.primary,
            onPressed: () {
              if (nameController.text.trim().isNotEmpty) {
                provider.updateWorkspace(
                  workspace.id,
                  nameController.text.trim(),
                  descController.text.trim().isEmpty ? null : descController.text.trim(),
                );
                Navigator.pop(ctx);
              }
            },
          ),
        ],
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ValoraTextField(
              controller: nameController,
              label: 'Workspace Name',
              prefixIcon: const Icon(Icons.workspaces_rounded, size: 20),
            ),
            const SizedBox(height: ValoraSpacing.md),
            ValoraTextField(
              controller: descController,
              label: 'Description',
              prefixIcon: const Icon(Icons.description_rounded, size: 20),
            ),
          ],
        ),
      ),
    );
  }

  void _showDeleteConfirmation(BuildContext context, WorkspaceProvider provider) {
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
          ValoraButton(
            label: 'Delete',
            variant: ValoraButtonVariant.primary, // Should be error variant if available, but consistent with pattern
            // Ideally add a specific style for destructive actions
            onPressed: () async {
                Navigator.pop(ctx); // Close dialog first
                try {
                  await provider.deleteWorkspace(widget.workspaceId);
                  if (context.mounted) {
                    Navigator.pop(context); // Go back to list
                  }
                } catch (e) {
                  if (context.mounted) {
                    ScaffoldMessenger.of(context).showSnackBar(
                      SnackBar(content: Text('Failed to delete workspace: $e')),
                    );
                  }
                }
            },
          ),
        ],
        child: Text(
          'Are you sure you want to delete this workspace? All saved listings, comments, and member associations will be permanently removed. This action cannot be undone.',
          style: ValoraTypography.bodyMedium,
        ),
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
  }
}
