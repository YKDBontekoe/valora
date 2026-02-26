import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:cached_network_image/cached_network_image.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_typography.dart';
import '../core/theme/valora_spacing.dart';
import '../providers/auth_provider.dart';
import '../providers/workspace_provider.dart';
import '../widgets/valora_widgets.dart';
import '../widgets/workspaces/activity_feed_widget.dart';
import '../models/activity_log.dart';
import '../models/workspace.dart';
import '../models/saved_listing.dart';
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

  void _showEditWorkspaceDialog(BuildContext context, Workspace workspace) {
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
                context.read<WorkspaceProvider>().updateWorkspace(
                      workspace.id,
                      nameController.text.trim(),
                      descController.text.trim().isEmpty
                          ? null
                          : descController.text.trim(),
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
              hint: 'Workspace Name',
              label: 'Name',
              prefixIcon: const Icon(Icons.workspaces_rounded, size: 20),
            ),
            const SizedBox(height: ValoraSpacing.md),
            ValoraTextField(
              controller: descController,
              hint: 'Description',
              label: 'Description',
              prefixIcon: const Icon(Icons.description_rounded, size: 20),
            ),
          ],
        ),
      ),
    );
  }

  void _showLeaveConfirmation(BuildContext context, String memberId) {
    showDialog(
      context: context,
      builder: (ctx) => ValoraDialog(
        title: 'Leave Workspace?',
        actions: [
          ValoraButton(
            label: 'Cancel',
            variant: ValoraButtonVariant.ghost,
            onPressed: () => Navigator.pop(ctx),
          ),
          ValoraButton(
            label: 'Leave',
            variant: ValoraButtonVariant.primary,
            onPressed: () {
              context.read<WorkspaceProvider>().leaveWorkspace(memberId);
              Navigator.pop(ctx);
              Navigator.pop(context); // Go back to list
            },
          ),
        ],
        child: const Text(
          'Are you sure you want to leave this workspace? You will lose access to all saved listings and comments.',
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final colorScheme = Theme.of(context).colorScheme;
    final currentUserEmail = context.select<AuthProvider, String?>((p) => p.email);

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
              final workspace = provider.selectedWorkspace;
              if (workspace == null || currentUserEmail == null) return const SizedBox.shrink();

              // Find current member
              final currentMember = provider.members.where((m) => m.email == currentUserEmail).firstOrNull;
              if (currentMember == null) return const SizedBox.shrink();

              final isOwner = currentMember.role == WorkspaceRole.owner;

              return PopupMenuButton<String>(
                icon: Icon(Icons.more_vert_rounded, color: colorScheme.onSurface),
                onSelected: (value) {
                  if (value == 'edit') {
                    _showEditWorkspaceDialog(context, workspace);
                  } else if (value == 'leave') {
                    _showLeaveConfirmation(context, currentMember.id);
                  }
                },
                itemBuilder: (context) => [
                  if (isOwner)
                    const PopupMenuItem(
                      value: 'edit',
                      child: Row(
                        children: [
                          Icon(Icons.edit_rounded, size: 20),
                          SizedBox(width: 8),
                          Text('Edit Workspace'),
                        ],
                      ),
                    ),
                  if (!isOwner)
                    const PopupMenuItem(
                      value: 'leave',
                      child: Row(
                        children: [
                          Icon(Icons.exit_to_app_rounded, color: ValoraColors.error, size: 20),
                          SizedBox(width: 8),
                          Text('Leave Workspace', style: TextStyle(color: ValoraColors.error)),
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

enum SavedListingSort { newest, priceHigh, priceLow, comments }

class _SavedListingsTab extends StatefulWidget {
  const _SavedListingsTab();

  @override
  State<_SavedListingsTab> createState() => _SavedListingsTabState();
}

class _SavedListingsTabState extends State<_SavedListingsTab> {
  final TextEditingController _searchController = TextEditingController();
  String _searchQuery = '';
  SavedListingSort _sortOption = SavedListingSort.newest;

  @override
  void initState() {
    super.initState();
    _searchController.addListener(() {
      setState(() {
        _searchQuery = _searchController.text;
      });
    });
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  List<SavedListing> _filterAndSort(List<SavedListing> listings) {
    var filtered = listings.where((s) {
      final address = s.listing?.address ?? '';
      final notes = s.notes ?? '';
      final query = _searchQuery.toLowerCase();
      return address.toLowerCase().contains(query) || notes.toLowerCase().contains(query);
    }).toList();

    filtered.sort((a, b) {
      switch (_sortOption) {
        case SavedListingSort.newest:
          return b.addedAt.compareTo(a.addedAt);
        case SavedListingSort.priceHigh:
          final priceA = a.listing?.price ?? 0;
          final priceB = b.listing?.price ?? 0;
          return priceB.compareTo(priceA);
        case SavedListingSort.priceLow:
          final priceA = a.listing?.price ?? 0;
          final priceB = b.listing?.price ?? 0;
          return priceA.compareTo(priceB);
        case SavedListingSort.comments:
          return b.commentCount.compareTo(a.commentCount);
      }
    });
    return filtered;
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.all(ValoraSpacing.md),
          child: Row(
            children: [
              Expanded(
                child: ValoraTextField(
                  controller: _searchController,
                  hint: 'Search saved listings...',
                  label: '',
                  prefixIcon: const Icon(Icons.search_rounded),
                ),
              ),
              const SizedBox(width: ValoraSpacing.sm),
              PopupMenuButton<SavedListingSort>(
                icon: const Icon(Icons.sort_rounded),
                onSelected: (val) => setState(() => _sortOption = val),
                itemBuilder: (context) => [
                  const PopupMenuItem(
                    value: SavedListingSort.newest,
                    child: Text('Newest Added'),
                  ),
                  const PopupMenuItem(
                    value: SavedListingSort.priceHigh,
                    child: Text('Price: High to Low'),
                  ),
                  const PopupMenuItem(
                    value: SavedListingSort.priceLow,
                    child: Text('Price: Low to High'),
                  ),
                  const PopupMenuItem(
                    value: SavedListingSort.comments,
                    child: Text('Most Comments'),
                  ),
                ],
              ),
            ],
          ),
        ),
        Expanded(
          child: Selector<WorkspaceProvider, List<SavedListing>>(
            selector: (_, p) => p.savedListings,
            builder: (context, savedListings, child) {
              final isDark = Theme.of(context).brightness == Brightness.dark;
              final displayList = _filterAndSort(savedListings);

              if (savedListings.isEmpty) {
                return Center(
                  child: ValoraEmptyState(
                    icon: Icons.bookmark_add_rounded,
                    title: 'No saved listings',
                    subtitle: 'Properties you save to this workspace will appear here.',
                  ),
                );
              }

              if (displayList.isEmpty) {
                return Center(
                  child: ValoraEmptyState(
                    icon: Icons.search_off_rounded,
                    title: 'No results',
                    subtitle: 'No listings match your search.',
                    actionLabel: 'Clear Search',
                    onAction: () => _searchController.clear(),
                  ),
                );
              }

              return ListView.separated(
                padding: const EdgeInsets.symmetric(horizontal: ValoraSpacing.md),
                itemCount: displayList.length,
                separatorBuilder: (_, _) => const SizedBox(height: ValoraSpacing.sm),
                itemBuilder: (context, index) {
                  final saved = displayList[index];
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
          ),
        ),
      ],
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
