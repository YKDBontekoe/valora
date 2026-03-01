import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_typography.dart';
import '../core/theme/valora_spacing.dart';
import '../models/saved_property.dart';
import '../models/comment.dart';
import '../providers/workspace_provider.dart';
import '../widgets/valora_widgets.dart';
import '../widgets/workspaces/comment_thread_widget.dart';

class SavedListingDetailScreen extends StatefulWidget {
  final SavedProperty savedListing;

  const SavedListingDetailScreen({super.key, required this.savedListing});

  @override
  State<SavedListingDetailScreen> createState() =>
      _SavedListingDetailScreenState();
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
      _commentsFuture = context
          .read<WorkspaceProvider>()
          .fetchComments(widget.savedListing.id);
    });
  }

  @override
  Widget build(BuildContext context) {
    final listing = widget.savedListing.property;
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final colorScheme = Theme.of(context).colorScheme;

    return Scaffold(
      backgroundColor: colorScheme.surface,
      body: CustomScrollView(
        slivers: [
          // Hero image / App bar
          SliverAppBar(
            expandedHeight: 0,
            pinned: true,
            backgroundColor: colorScheme.surface.withValues(alpha: 0.95),
            surfaceTintColor: Colors.transparent,
            title: Text(
              listing?.address ?? 'Listing Details',
              style: ValoraTypography.titleMedium.copyWith(
                fontWeight: FontWeight.bold,
                color: colorScheme.onSurface,
              ),
            ),
            flexibleSpace: null,
          ),

          // Property info card
          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.all(ValoraSpacing.md),
              child: ValoraCard(
                padding: const EdgeInsets.all(ValoraSpacing.lg),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      listing?.address ?? 'Unknown Address',
                      style: ValoraTypography.headlineSmall.copyWith(
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    if (listing?.city != null &&
                        listing!.city!.isNotEmpty) ...[
                      const SizedBox(height: 4),
                      Row(
                        children: [
                          Icon(Icons.location_on_outlined,
                              size: 16,
                              color: isDark
                                  ? ValoraColors.neutral400
                                  : ValoraColors.neutral500),
                          const SizedBox(width: 4),
                          Text(
                            listing.city!,
                            style: ValoraTypography.bodyMedium.copyWith(
                              color: isDark
                                  ? ValoraColors.neutral400
                                  : ValoraColors.neutral500,
                            ),
                          ),
                        ],
                      ),
                    ],
                    if (listing?.livingAreaM2 != null) ...[
                      const SizedBox(height: ValoraSpacing.md),
                      Row(
                        children: [
                          _InfoChip(
                            icon: Icons.square_foot_rounded,
                            label: '${listing!.livingAreaM2} m²',
                            isDark: isDark,
                          ),
                        ],
                      ),
                    ],
                  ],
                ),
              ),
            ),
          ),

          // Notes section
          if (widget.savedListing.notes != null &&
              widget.savedListing.notes!.isNotEmpty)
            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.symmetric(
                    horizontal: ValoraSpacing.md),
                child: ValoraCard(
                  padding: const EdgeInsets.all(ValoraSpacing.md),
                  borderColor: ValoraColors.primary.withValues(alpha: 0.15),
                  backgroundColor:
                      ValoraColors.primary.withValues(alpha: isDark ? 0.08 : 0.03),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Icon(Icons.sticky_note_2_rounded,
                          size: 20,
                          color: isDark
                              ? ValoraColors.primaryLight
                              : ValoraColors.primary),
                      const SizedBox(width: ValoraSpacing.sm),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              'Notes',
                              style: ValoraTypography.labelMedium.copyWith(
                                fontWeight: FontWeight.bold,
                                color: isDark
                                    ? ValoraColors.primaryLight
                                    : ValoraColors.primary,
                              ),
                            ),
                            const SizedBox(height: 4),
                            Text(
                              widget.savedListing.notes!,
                              style: ValoraTypography.bodyMedium.copyWith(
                                color: isDark
                                    ? ValoraColors.neutral200
                                    : ValoraColors.neutral700,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),

          // Comments section header
          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(
                  ValoraSpacing.lg, ValoraSpacing.lg, ValoraSpacing.lg, ValoraSpacing.sm),
              child: Text(
                'Comments',
                style: ValoraTypography.titleMedium.copyWith(
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
          ),

          // Comments
          SliverFillRemaining(
            hasScrollBody: true,
            child: FutureBuilder<List<Comment>>(
              future: _commentsFuture,
              builder: (context, snapshot) {
                if (snapshot.connectionState == ConnectionState.waiting) {
                  return const Center(
                    child: ValoraLoadingIndicator(
                        message: 'Loading comments...'),
                  );
                }
                if (snapshot.hasError) {
                  return Center(
                    child: ValoraEmptyState(
                      icon: Icons.error_outline_rounded,
                      title: 'Failed to load comments',
                      subtitle: 'Please try again.',
                      actionLabel: 'Retry',
                      onAction: _refreshComments,
                    ),
                  );
                }
                final comments = snapshot.data ?? [];
                if (comments.isEmpty) {
                  return Center(
                    child: ValoraEmptyState(
                      icon: Icons.chat_bubble_outline_rounded,
                      title: 'No comments yet',
                      subtitle: 'Start a conversation about this property.',
                    ),
                  );
                }
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

class _InfoChip extends StatelessWidget {
  final IconData icon;
  final String label;
  final bool isDark;

  const _InfoChip({
    required this.icon,
    required this.label,
    required this.isDark,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: isDark
            ? ValoraColors.neutral800
            : ValoraColors.neutral100,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 16,
              color: isDark
                  ? ValoraColors.neutral300
                  : ValoraColors.neutral600),
          const SizedBox(width: 4),
          Text(
            label,
            style: ValoraTypography.labelSmall.copyWith(
              color: isDark
                  ? ValoraColors.neutral300
                  : ValoraColors.neutral600,
              fontWeight: FontWeight.w500,
            ),
          ),
        ],
      ),
    );
  }
}
