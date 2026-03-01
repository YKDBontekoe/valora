import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../core/theme/valora_colors.dart';
import '../../../providers/context_report_provider.dart';
import '../../../providers/workspace_provider.dart';

class SaveToWorkspaceButton extends StatelessWidget {
  final ContextReportProvider provider;

  const SaveToWorkspaceButton({super.key, required this.provider});

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: () => _showWorkspacePicker(context),
        borderRadius: BorderRadius.circular(12),
        child: Container(
          width: 44,
          height: 44,
          decoration: BoxDecoration(
            border: Border.all(color: ValoraColors.neutral200),
            borderRadius: BorderRadius.circular(12),
          ),
          child: const Icon(
            Icons.add_business_rounded,
            color: ValoraColors.neutral500,
            size: 22,
          ),
        ),
      ),
    );
  }

  void _showWorkspacePicker(BuildContext context) {
    final workspaceProvider = context.read<WorkspaceProvider>();
    
    // Ensure workspaces are loaded
    workspaceProvider.fetchWorkspaces();

    showModalBottomSheet(
      context: context,
      backgroundColor: Colors.transparent,
      builder: (context) => Container(
        decoration: BoxDecoration(
          color: Theme.of(context).colorScheme.surface,
          borderRadius: const BorderRadius.vertical(top: Radius.circular(20)),
        ),
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Save to Workspace',
              style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 8),
            Text(
              'Select a workspace to save this context report.',
              style: Theme.of(context).textTheme.bodyMedium?.copyWith(color: ValoraColors.neutral500),
            ),
            const SizedBox(height: 24),
            Flexible(
              child: Consumer<WorkspaceProvider>(
                builder: (context, wp, child) {
                  if (wp.isWorkspacesLoading) {
                    return const Center(child: CircularProgressIndicator());
                  }
                  if (wp.workspaces.isEmpty) {
                    return const Center(child: Text('No workspaces found. Create one first!'));
                  }
                  return ListView.separated(
                    shrinkWrap: true,
                    itemCount: wp.workspaces.length,
                    separatorBuilder: (context, index) => const Divider(),
                    itemBuilder: (context, index) {
                      final ws = wp.workspaces[index];
                      return ListTile(
                        leading: const Icon(Icons.workspaces_rounded, color: ValoraColors.primary),
                        title: Text(ws.name),
                        onTap: () async {
                          final messenger = ScaffoldMessenger.of(context);
                          Navigator.pop(context);
                          try {
                            // We need a backend endpoint that accepts ContextReportDto + WorkspaceId
                            // For now, we'll use the ID of the property if we had one, 
                            // but in Phase 2 we'll use SaveContextReportAsync
                            messenger.showSnackBar(
                              SnackBar(content: Text('Saving to ${ws.name}...'))
                            );
                            
                            // Implementation detail: The provider would call the repo
                            // which calls the NEW POST /api/workspaces/{id}/properties/from-report
                          } catch (e) {
                            messenger.showSnackBar(
                              SnackBar(content: Text('Error: $e'))
                            );
                          }
                        },
                      );
                    },
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}
