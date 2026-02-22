import 'package:flutter/material.dart';
import '../../models/saved_listing.dart';
import '../../providers/workspace_provider.dart';
import '../../models/comment.dart';
import '../widgets/workspaces/comment_thread_widget.dart';
import 'package:provider/provider.dart';

class SavedListingDetailScreen extends StatefulWidget {
  final SavedListing savedListing;

  const SavedListingDetailScreen({super.key, required this.savedListing});

  @override
  State<SavedListingDetailScreen> createState() => _SavedListingDetailScreenState();
}

class _SavedListingDetailScreenState extends State<SavedListingDetailScreen> {
  late Future<List<Comment>> _commentsFuture;

  @override
  void initState() {
    super.initState();
    _refreshComments();
  }

  void _refreshComments() {
    setState(() {
       _commentsFuture = context.read<WorkspaceProvider>().fetchComments(widget.savedListing.id);
    });
  }

  @override
  Widget build(BuildContext context) {
    final listing = widget.savedListing.listing;
    return Scaffold(
      appBar: AppBar(title: Text(listing?.address ?? 'Listing Details')),
      body: Column(
        children: [
          if (listing?.imageUrl != null)
            Image.network(listing!.imageUrl!, height: 200, width: double.infinity, fit: BoxFit.cover),
          Padding(
            padding: const EdgeInsets.all(16.0),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(listing?.address ?? '', style: Theme.of(context).textTheme.headlineSmall),
                Text('${listing?.city ?? ""} • ${listing?.price != null ? "€${listing!.price}" : ""}'),
                const SizedBox(height: 8),
                Text('Notes: ${widget.savedListing.notes ?? "None"}'),
              ],
            ),
          ),
          const Divider(),
          const Padding(
            padding: EdgeInsets.symmetric(horizontal: 16.0),
            child: Text('Comments', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
          ),
          Expanded(
            child: FutureBuilder<List<Comment>>(
              future: _commentsFuture,
              builder: (context, snapshot) {
                if (snapshot.connectionState == ConnectionState.waiting) {
                  return const Center(child: CircularProgressIndicator());
                }
                if (snapshot.hasError) {
                  return Center(child: Text('Error: ${snapshot.error}'));
                }
                final comments = snapshot.data ?? [];
                return CommentThreadWidget(
                  savedListingId: widget.savedListing.id,
                  comments: comments,
                  onRefresh: _refreshComments,
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}
