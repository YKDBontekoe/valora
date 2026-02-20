import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_typography.dart';
import '../providers/context_report_provider.dart';
import '../providers/saved_properties_provider.dart';
import '../widgets/common/valora_loading_indicator.dart';
import '../widgets/common/valora_empty_state.dart';
import '../widgets/valora_glass_container.dart';
import 'home_screen.dart'; // Import HomeScreen to access its state

class SavedPropertiesScreen extends StatefulWidget {
  const SavedPropertiesScreen({super.key});

  @override
  State<SavedPropertiesScreen> createState() => _SavedPropertiesScreenState();
}

class _SavedPropertiesScreenState extends State<SavedPropertiesScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<SavedPropertiesProvider>().fetchProperties();
    });
  }

  void _onPropertyTap(String address) {
    // Generate report
    context.read<ContextReportProvider>().generate(address);

    // Switch to Search/Report tab (Index 0)
    // We attempt to find the HomeScreenState to switch tabs.
    final homeState = context.findAncestorStateOfType<HomeScreenState>();
    if (homeState != null) {
      homeState.switchToTab(0);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      extendBodyBehindAppBar: true,
      appBar: AppBar(
        title: Text('Saved Properties', style: ValoraTypography.titleMedium),
        centerTitle: true,
        backgroundColor: Colors.transparent,
        elevation: 0,
        surfaceTintColor: Colors.transparent,
      ),
      body: Consumer<SavedPropertiesProvider>(
        builder: (context, provider, child) {
          if (provider.isLoading && provider.properties.isEmpty) {
            return const Center(child: ValoraLoadingIndicator());
          }

          if (provider.error != null) {
            return Center(
              child: ValoraEmptyState(
                title: 'Error Loading Properties',
                subtitle: provider.error ?? 'Unknown error',
                icon: Icons.error_outline_rounded,
                actionLabel: 'Retry',
                onAction: provider.fetchProperties,
              ),
            );
          }

          if (provider.properties.isEmpty) {
            return const Center(
              child: ValoraEmptyState(
                title: 'No Saved Properties',
                subtitle: 'Properties you save will appear here.',
                icon: Icons.favorite_border_rounded,
              ),
            );
          }

          return ListView.separated(
            padding: const EdgeInsets.fromLTRB(20, 100, 20, 100),
            itemCount: provider.properties.length,
            separatorBuilder: (context, index) => const SizedBox(height: 16),
            itemBuilder: (context, index) {
              final property = provider.properties[index];
              return Dismissible(
                key: Key(property.id),
                direction: DismissDirection.endToStart,
                background: Container(
                  alignment: Alignment.centerRight,
                  padding: const EdgeInsets.only(right: 20),
                  decoration: BoxDecoration(
                    color: ValoraColors.error,
                    borderRadius: BorderRadius.circular(16),
                  ),
                  child: const Icon(Icons.delete_outline_rounded, color: Colors.white),
                ),
                confirmDismiss: (direction) async {
                  return await showDialog(
                    context: context,
                    builder: (BuildContext context) {
                      return AlertDialog(
                        title: const Text("Confirm"),
                        content: const Text("Are you sure you want to remove this property?"),
                        actions: <Widget>[
                          TextButton(
                            onPressed: () => Navigator.of(context).pop(false),
                            child: const Text("Cancel"),
                          ),
                          TextButton(
                            onPressed: () => Navigator.of(context).pop(true),
                            child: const Text("Delete", style: TextStyle(color: Colors.red)),
                          ),
                        ],
                      );
                    },
                  );
                },
                onDismissed: (direction) {
                  provider.deleteProperty(property.id);
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(content: Text('Property removed')),
                  );
                },
                child: GestureDetector(
                  onTap: () => _onPropertyTap(property.address),
                  child: ValoraGlassContainer(
                    padding: const EdgeInsets.all(16),
                    child: Row(
                      children: [
                        Container(
                          width: 50,
                          height: 50,
                          decoration: BoxDecoration(
                            color: Theme.of(context).colorScheme.primary.withOpacity(0.1),
                            borderRadius: BorderRadius.circular(12),
                          ),
                          child: Icon(
                            Icons.home_rounded,
                            color: Theme.of(context).colorScheme.primary,
                          ),
                        ),
                        const SizedBox(width: 16),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                property.address,
                                style: ValoraTypography.bodyMedium.copyWith(fontWeight: FontWeight.w600),
                                maxLines: 2,
                                overflow: TextOverflow.ellipsis,
                              ),
                              if (property.cachedScore != null) ...[
                                const SizedBox(height: 4),
                                Container(
                                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                                  decoration: BoxDecoration(
                                    color: ValoraColors.success.withOpacity(0.1),
                                    borderRadius: BorderRadius.circular(4),
                                  ),
                                  child: Text(
                                    'Score: ${property.cachedScore}',
                                    style: ValoraTypography.labelSmall.copyWith(color: ValoraColors.success),
                                  ),
                                ),
                              ],
                            ],
                          ),
                        ),
                        const Icon(Icons.chevron_right_rounded, color: ValoraColors.neutral400),
                      ],
                    ),
                  ),
                ),
              );
            },
          );
        },
      ),
    );
  }
}
