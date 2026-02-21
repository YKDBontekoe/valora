import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/workspace_provider.dart';
import '../widgets/workspaces/activity_feed_widget.dart';
import '../widgets/workspaces/member_management_widget.dart';
import 'saved_listing_detail_screen.dart';

class WorkspaceDetailScreen extends StatefulWidget {
  final String workspaceId;

  const WorkspaceDetailScreen({super.key, required this.workspaceId});

  @override
  State<WorkspaceDetailScreen> createState() => _WorkspaceDetailScreenState();
}

class _WorkspaceDetailScreenState extends State<WorkspaceDetailScreen> with SingleTickerProviderStateMixin {
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
    return Scaffold(
      appBar: AppBar(
        title: Consumer<WorkspaceProvider>(
          builder: (_, p, __) => Text(p.selectedWorkspace?.name ?? 'Workspace'),
        ),
        bottom: TabBar(
          controller: _tabController,
          tabs: const [
            Tab(text: 'Saved'),
            Tab(text: 'Members'),
            Tab(text: 'Activity'),
          ],
        ),
      ),
      body: Consumer<WorkspaceProvider>(
        builder: (context, provider, child) {
          if (provider.isLoading) {
            return const Center(child: CircularProgressIndicator());
          }
          if (provider.error != null) {
            return Center(child: Text('Error: ${provider.error}'));
          }

          return TabBarView(
            controller: _tabController,
            children: [
              _buildSavedListings(provider),
              MemberManagementWidget(
                members: provider.members,
                canInvite: true, // Should check permission properly in production
              ),
              ActivityFeedWidget(activities: provider.activityLogs),
            ],
          );
        },
      ),
    );
  }

  Widget _buildSavedListings(WorkspaceProvider provider) {
    if (provider.savedListings.isEmpty) {
      return const Center(child: Text('No saved listings yet.'));
    }
    return ListView.builder(
      itemCount: provider.savedListings.length,
      itemBuilder: (context, index) {
        final saved = provider.savedListings[index];
        final listing = saved.listing;
        return Card(
          margin: const EdgeInsets.all(8),
          child: ListTile(
            leading: listing?.imageUrl != null
                ? Image.network(listing!.imageUrl!, width: 50, height: 50, fit: BoxFit.cover)
                : const Icon(Icons.home),
            title: Text(listing?.address ?? 'Unknown Address'),
            subtitle: Text('${listing?.city ?? ""} • ${saved.notes ?? "No notes"}'),
            trailing: Text('${saved.commentCount} comments'),
            onTap: () {
               Navigator.push(context, MaterialPageRoute(builder: (_) => SavedListingDetailScreen(savedListing: saved)));
            },
          ),
        );
      },
    );
  }
}
