import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_typography.dart';
import '../core/theme/valora_spacing.dart';
import '../models/workspace.dart';
import '../providers/workspace_provider.dart';
import '../widgets/valora_widgets.dart';
import '../widgets/workspaces/workspace_list_item.dart';

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
          Selector<WorkspaceProvider,
              ({bool isLoading, String? error, List<Workspace> workspaces})>(
            selector: (_, p) => (
              isLoading: p.isWorkspacesLoading,
              error: p.error,
              workspaces: p.workspaces
            ),
            builder: (context, data, child) {
              if (data.isLoading && data.workspaces.isEmpty) {
                return SliverPadding(
                  padding: const EdgeInsets.all(ValoraSpacing.md),
                  sliver: SliverList(
                    delegate: SliverChildBuilderDelegate(
                      (context, index) {
                        if (index.isOdd) {
                          return const SizedBox(height: ValoraSpacing.sm);
                        }
                        return const WorkspaceListSkeleton();
                      },
                      childCount: 9, // 5 items + 4 separators
                    ),
                  ),
                );
              }
              if (data.error != null && data.workspaces.isEmpty) {
                return SliverFillRemaining(
                  hasScrollBody: false,
                  child: Center(
                    child: ValoraEmptyState(
                      icon: Icons.error_outline_rounded,
                      title: 'Failed to load',
                      subtitle: 'Could not load your workspaces. Please try again.',
                      actionLabel: 'Retry',
                      onAction: () =>
                          context.read<WorkspaceProvider>().fetchWorkspaces(),
                    ),
                  ),
                );
              }

              final displayList = _getFilteredAndSortedWorkspaces(data.workspaces);

              if (displayList.isEmpty) {
                if (_searchQuery.isNotEmpty) {
                  return SliverFillRemaining(
                    hasScrollBody: false,
                    child: Center(
                      child: ValoraEmptyState(
                        icon: Icons.search_off_rounded,
                        title: 'No results',
                        subtitle: 'No workspaces match your search.',
                        actionLabel: 'Clear Search',
                        onAction: () => _searchController.clear(),
                      ),
                    ),
                  );
                }
                return SliverFillRemaining(
                  hasScrollBody: false,
                  child: Center(
                    child: ValoraEmptyState(
                      icon: Icons.workspaces_rounded,
                      title: 'No workspaces yet',
                      subtitle:
                          'Create a workspace to collaborate with family and friends on your property search.',
                      actionLabel: 'Create Workspace',
                      onAction: () => _showCreateDialog(context),
                    ),
                  ),
                );
              }
              return SliverPadding(
                padding: const EdgeInsets.all(ValoraSpacing.md),
                sliver: SliverList(
                  delegate: SliverChildBuilderDelegate(
                    (context, index) {
                      if (index.isOdd) {
                        return const SizedBox(height: ValoraSpacing.sm);
                      }
                      final workspace = displayList[index ~/ 2];
                      return WorkspaceListItem(
                        workspace: workspace,
                        index: index ~/ 2,
                      );
                    },
                    childCount: displayList.length * 2 - 1,
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

