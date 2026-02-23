import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_typography.dart';
import '../core/theme/valora_spacing.dart';
import '../models/workspace.dart';
import '../providers/workspace_provider.dart';
import '../widgets/valora_widgets.dart';
import 'workspace_detail_screen.dart';

class WorkspaceListScreen extends StatefulWidget {
  const WorkspaceListScreen({super.key});

  @override
  State<WorkspaceListScreen> createState() => _WorkspaceListScreenState();
}

class _WorkspaceListScreenState extends State<WorkspaceListScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<WorkspaceProvider>().fetchWorkspaces();
    });
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
        title: Text(
          'Workspaces',
          style: ValoraTypography.headlineMedium.copyWith(
            color: colorScheme.onSurface,
            fontWeight: FontWeight.bold,
          ),
        ),
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => _showCreateDialog(context),
        backgroundColor: ValoraColors.primary,
        foregroundColor: Colors.white,
        icon: const Icon(Icons.add_rounded),
        label: const Text('New Workspace'),
      ),
      body: Selector<WorkspaceProvider, ({bool isLoading, String? error, List<Workspace> workspaces})>(
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
          if (data.workspaces.isEmpty) {
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
            itemCount: data.workspaces.length,
            separatorBuilder: (_, _) =>
                const SizedBox(height: ValoraSpacing.sm),
            itemBuilder: (context, index) {
              final workspace = data.workspaces[index];
              return ValoraCard(
                onTap: () {
                  Navigator.push(
                    context,
                    MaterialPageRoute(
                      builder: (_) => ChangeNotifierProvider.value(
                        value: context.read<WorkspaceProvider>(),
                        child: WorkspaceDetailScreen(
                            workspaceId: workspace.id),
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
                          workspace.name.isNotEmpty
                              ? workspace.name[0].toUpperCase()
                              : 'W',
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
