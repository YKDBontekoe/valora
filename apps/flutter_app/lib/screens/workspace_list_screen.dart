import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_typography.dart';
import '../core/theme/valora_spacing.dart';
import '../models/workspace.dart';
import '../providers/workspace_provider.dart';
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
      appBar: AppBar(
        backgroundColor: colorScheme.surface.withValues(alpha: 0.95),
        surfaceTintColor: Colors.transparent,
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
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => _showCreateDialog(context),
        backgroundColor: ValoraColors.primary,
        foregroundColor: Colors.white,
        icon: const Icon(Icons.add_rounded),
        label: const Text('New Workspace'),
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(
              ValoraSpacing.md,
              ValoraSpacing.sm,
              ValoraSpacing.md,
              ValoraSpacing.sm,
            ),
            child: ValoraTextField(
              controller: _searchController,
              hint: 'Search workspaces...',
              label: '', // No label needed for search bar usually
              prefixIcon: const Icon(Icons.search_rounded),
            ),
          ),
          Expanded(
            child: Selector<WorkspaceProvider, ({bool isLoading, String? error, List<Workspace> workspaces})>(
              selector: (_, provider) => (isLoading: provider.isWorkspacesLoading, error: provider.error, workspaces: provider.workspaces),
              builder: (context, data, child) {
                if (data.isLoading && data.workspaces.isEmpty) {
                  return const Center(
                    child: ValoraLoadingIndicator(message: 'Loading workspaces...'),
                  );
                }
                if (data.error != null && data.workspaces.isEmpty) {
                  return Center(
                    child: ValoraEmptyState(
                      icon: Icons.error_outline_rounded,
                      title: 'Failed to load',
                      subtitle: 'Could not load your workspaces. Please try again.',
                      actionLabel: 'Retry',
                      onAction: () => context.read<WorkspaceProvider>().fetchWorkspaces(),
                    ),
                  );
                }

                final displayList = _getFilteredAndSortedWorkspaces(data.workspaces);

                if (displayList.isEmpty) {
                  if (_searchQuery.isNotEmpty) {
                    return Center(
                      child: ValoraEmptyState(
                        icon: Icons.search_off_rounded,
                        title: 'No results',
                        subtitle: 'No workspaces match your search.',
                        actionLabel: 'Clear Search',
                        onAction: () => _searchController.clear(),
                      ),
                    );
                  }
                  return Center(
                    child: ValoraEmptyState(
                      icon: Icons.workspaces_rounded,
                      title: 'No workspaces yet',
                      subtitle:
                          'Create a workspace to collaborate with family and friends on your property search.',
                      actionLabel: 'Create Workspace',
                      onAction: () => _showCreateDialog(context),
                    ),
                  );
                }
                return ListView.separated(
                  padding: const EdgeInsets.all(ValoraSpacing.md),
                  itemCount: displayList.length,
                  separatorBuilder: (_, _) =>
                      const SizedBox(height: ValoraSpacing.sm),
                  itemBuilder: (context, index) {
                    final workspace = displayList[index];
                    return WorkspaceListItem(workspace: workspace);
                  },
                );
              },
            ),
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

  const WorkspaceListItem({super.key, required this.workspace});

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
          Container(
            width: 48,
            height: 48,
            decoration: BoxDecoration(
              color: ValoraColors.primary.withValues(alpha: 0.1),
              borderRadius: BorderRadius.circular(14),
            ),
            child: Center(
              child: Text(
                workspace.name.isNotEmpty ? workspace.name[0].toUpperCase() : 'W',
                style: ValoraTypography.titleLarge.copyWith(
                  color: ValoraColors.primary,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
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
    );
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
