import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:provider/provider.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_typography.dart';
import '../core/theme/valora_spacing.dart';
import '../models/workspace.dart';
import '../providers/workspace_provider.dart';
import '../widgets/valora_error_state.dart';
import '../widgets/valora_widgets.dart';
import 'workspace_detail_screen.dart';

enum SortOption { name, createdDate, memberCount }

class WorkspaceListScreen extends StatefulWidget {
  const WorkspaceListScreen({super.key});

  @override
  State<WorkspaceListScreen> createState() => _WorkspaceListScreenState();
}

class _WorkspaceListScreenState extends State<WorkspaceListScreen> {
  final TextEditingController _searchController = TextEditingController();
  String _searchQuery = '';
  SortOption _sortOption = SortOption.createdDate;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<WorkspaceProvider>().fetchWorkspaces();
    });
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

  List<Workspace> _getFilteredAndSortedWorkspaces(List<Workspace> workspaces) {
    var filtered = workspaces.where((w) {
      return w.name.toLowerCase().contains(_searchQuery.toLowerCase());
    }).toList();

    filtered.sort((a, b) {
      switch (_sortOption) {
        case SortOption.name:
          return a.name.compareTo(b.name);
        case SortOption.createdDate:
          return b.createdAt.compareTo(a.createdAt);
        case SortOption.memberCount:
          return b.memberCount.compareTo(a.memberCount);
      }
    });

    return filtered;
  }

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Scaffold(
      backgroundColor: colorScheme.surface,
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => _showCreateDialog(context),
        backgroundColor: ValoraColors.primary,
        foregroundColor: Colors.white,
        icon: const Icon(Icons.add_rounded),
        label: const Text('New Workspace'),
      ),
      body: CustomScrollView(
        slivers: [
          SliverAppBar(
            backgroundColor: colorScheme.surface.withValues(alpha: 0.95),
            surfaceTintColor: Colors.transparent,
            pinned: true,
            title: Text(
              'Workspaces',
              style: ValoraTypography.headlineMedium.copyWith(
                color: colorScheme.onSurface,
                fontWeight: FontWeight.bold,
              ),
            ),
            actions: [
              PopupMenuButton<SortOption>(
                icon: Icon(Icons.sort_rounded, color: colorScheme.onSurface),
                onSelected: (option) {
                  setState(() {
                    _sortOption = option;
                  });
                },
                itemBuilder: (context) => [
                  const PopupMenuItem(
                    value: SortOption.createdDate,
                    child: Text('Newest First'),
                  ),
                  const PopupMenuItem(
                    value: SortOption.name,
                    child: Text('Name (A-Z)'),
                  ),
                  const PopupMenuItem(
                    value: SortOption.memberCount,
                    child: Text('Member Count'),
                  ),
                ],
              ),
            ],
          ),
          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(
                ValoraSpacing.md,
                ValoraSpacing.sm,
                ValoraSpacing.md,
                ValoraSpacing.sm,
              ),
              child: ValoraSearchField(
                controller: _searchController,
                hintText: 'Search workspaces...',
              ),
            ),
          ),
          Selector<WorkspaceProvider, ({bool isLoading, Object? exception, List<Workspace> workspaces})>(
            selector: (_, provider) => (
              isLoading: provider.isWorkspacesLoading,
              exception: provider.exception,
              workspaces: provider.workspaces
            ),
            builder: (context, data, child) {
              // 1. Loading State with Skeletons
              if (data.isLoading && data.workspaces.isEmpty) {
                return SliverPadding(
                  padding: const EdgeInsets.all(ValoraSpacing.md),
                  sliver: SliverList(
                    delegate: SliverChildBuilderDelegate(
                      (context, index) {
                        if (index.isOdd) return const SizedBox(height: ValoraSpacing.sm);
                        return const _WorkspaceListSkeleton();
                      },
                      childCount: 9,
                    ),
                  ),
                );
              }

              // 2. Error State
              if (data.exception != null && data.workspaces.isEmpty) {
                return SliverFillRemaining(
                  hasScrollBody: false,
                  child: Center(
                    child: ValoraErrorState(
                      error: data.exception!,
                      onRetry: () => context.read<WorkspaceProvider>().fetchWorkspaces(),
                    ),
                  ),
                );
              }

              final displayList = _getFilteredAndSortedWorkspaces(data.workspaces);

              // 3. Empty States
              if (displayList.isEmpty) {
                return SliverFillRemaining(
                  hasScrollBody: false,
                  child: Center(
                    child: ValoraEmptyState(
                      icon: _searchQuery.isNotEmpty 
                          ? Icons.search_off_rounded 
                          : Icons.workspaces_rounded,
                      title: _searchQuery.isNotEmpty ? 'No results' : 'No workspaces yet',
                      subtitle: _searchQuery.isNotEmpty
                          ? 'No workspaces match your search.'
                          : 'Create a workspace to collaborate with family and friends.',
                      actionLabel: _searchQuery.isNotEmpty ? 'Clear Search' : 'Create Workspace',
                      onAction: () => _searchQuery.isNotEmpty 
                          ? _searchController.clear() 
                          : _showCreateDialog(context),
                    ),
                  ),
                );
              }

              // 4. Content List
              return SliverPadding(
                padding: const EdgeInsets.all(ValoraSpacing.md),
                sliver: SliverList(
                  delegate: SliverChildBuilderDelegate(
                    (context, index) {
                      if (index.isOdd) return const SizedBox(height: ValoraSpacing.sm);
                      final workspace = displayList[index ~/ 2];
                      return WorkspaceListItem(
                        workspace: workspace,
                        index: index ~/ 2,
                      );
                    },
                    childCount: (displayList.length * 2 - 1).clamp(0, double.infinity).toInt(),
                  ),
                ),
              );
            },
          ),
        ],
      ),
    );
  }

  void _showCreateDialog(BuildContext context) {
    final nameController = TextEditingController();
    final descController = TextEditingController();
    showDialog(
      context: context,
      builder: (ctx) => ValoraDialog(
        title: 'New Workspace',
        actions: [
          ValoraButton(
            label: 'Cancel',
            variant: ValoraButtonVariant.ghost,
            onPressed: () => Navigator.pop(ctx),
          ),
          ValoraButton(
            label: 'Create',
            variant: ValoraButtonVariant.primary,
            onPressed: () {
              if (nameController.text.trim().isNotEmpty) {
                context.read<WorkspaceProvider>().createWorkspace(
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
              hint: 'e.g. Amsterdam House Hunt',
              label: 'Workspace Name',
              prefixIcon: const Icon(Icons.workspaces_rounded, size: 20),
            ),
            const SizedBox(height: ValoraSpacing.md),
            ValoraTextField(
              controller: descController,
              hint: 'Optional description...',
              label: 'Description',
              prefixIcon: const Icon(Icons.description_rounded, size: 20),
            ),
          ],
        ),
      ),
    );
  }
}

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
            'Are you sure you want to delete "${workspace.name}"? This action cannot be undone.'),
      ),
    );
  }
}

class _WorkspaceListSkeleton extends StatelessWidget {
  const _WorkspaceListSkeleton();

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