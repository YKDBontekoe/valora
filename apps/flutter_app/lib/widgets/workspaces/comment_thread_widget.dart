import 'package:flutter/material.dart';
import '../../models/comment.dart';
import 'package:provider/provider.dart';
import '../../providers/workspace_provider.dart';
import 'package:intl/intl.dart';
import '../valora_widgets.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';

class CommentThreadWidget extends StatefulWidget {
  final String savedListingId;
  final List<Comment> comments;
  final VoidCallback? onRefresh;

  const CommentThreadWidget({super.key, required this.savedListingId, required this.comments, this.onRefresh});

  @override
  State<CommentThreadWidget> createState() => _CommentThreadWidgetState();
}

class _CommentThreadWidgetState extends State<CommentThreadWidget> {
  final _controller = TextEditingController();
  String? _replyToId;
  bool _isSubmitting = false;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Expanded(
          child: widget.comments.isEmpty
          ? Center(
              child: ValoraEmptyState(
                icon: Icons.chat_bubble_outline_rounded,
                title: 'No comments yet',
                subtitle: 'Start a conversation about this property.',
              ),
            )
          : ListView.builder(
              padding: const EdgeInsets.symmetric(horizontal: ValoraSpacing.md, vertical: ValoraSpacing.sm),
              itemCount: widget.comments.length,
              itemBuilder: (context, index) => _buildComment(widget.comments[index]),
            ),
        ),
        _buildInput(),
      ],
    );
  }

  Widget _buildComment(Comment comment, {int depth = 0}) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Padding(
      padding: EdgeInsets.only(
        top: ValoraSpacing.sm,
        bottom: ValoraSpacing.sm,
        left: depth > 0 ? ValoraSpacing.xl : 0,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              ValoraAvatar(
                initials: comment.userId.isNotEmpty ? comment.userId[0].toUpperCase() : '?',
                size: ValoraAvatarSize.small,
              ),
              const SizedBox(width: ValoraSpacing.sm),
              Expanded(
                child: ValoraCard(
                  padding: const EdgeInsets.all(ValoraSpacing.md),
                  backgroundColor: isDark
                      ? ValoraColors.neutral800
                      : (depth > 0 ? ValoraColors.neutral50 : ValoraColors.surfaceLight),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Text(
                            comment.userId, // Optionally map to user name
                            style: ValoraTypography.titleSmall.copyWith(
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                          Text(
                            DateFormat.yMMMd().add_jm().format(comment.createdAt),
                            style: ValoraTypography.labelSmall.copyWith(
                              color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: ValoraSpacing.xs),
                      Text(
                        comment.content,
                        style: ValoraTypography.bodyMedium.copyWith(
                          color: isDark ? ValoraColors.neutral200 : ValoraColors.neutral800,
                        ),
                      ),
                      const SizedBox(height: ValoraSpacing.sm),
                      InkWell(
                        onTap: () => setState(() => _replyToId = comment.id),
                        borderRadius: BorderRadius.circular(ValoraSpacing.radiusSm),
                        child: Padding(
                          padding: const EdgeInsets.symmetric(vertical: 4, horizontal: 8),
                          child: Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              Icon(
                                Icons.reply_rounded,
                                size: 16,
                                color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500
                              ),
                              const SizedBox(width: 4),
                              Text(
                                'Reply',
                                style: ValoraTypography.labelSmall.copyWith(
                                  color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ),
          if (comment.replies.isNotEmpty)
            ...comment.replies.map((r) => _buildComment(r, depth: depth + 1)),
        ],
      ),
    );
  }

  Widget _buildInput() {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Container(
      padding: EdgeInsets.fromLTRB(
        ValoraSpacing.md,
        ValoraSpacing.sm,
        ValoraSpacing.md,
        ValoraSpacing.md + MediaQuery.of(context).padding.bottom
      ),
      decoration: BoxDecoration(
        color: isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight,
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.05),
            blurRadius: 10,
            offset: const Offset(0, -5),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (_replyToId != null)
            Padding(
              padding: const EdgeInsets.only(bottom: ValoraSpacing.sm),
              child: Row(
                children: [
                  Icon(
                    Icons.reply_rounded,
                    size: 16,
                    color: isDark ? ValoraColors.primaryLight : ValoraColors.primary
                  ),
                  const SizedBox(width: ValoraSpacing.xs),
                  Text(
                    'Replying to comment',
                    style: ValoraTypography.labelSmall.copyWith(
                      color: isDark ? ValoraColors.primaryLight : ValoraColors.primary,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  const Spacer(),
                  InkWell(
                    onTap: () => setState(() => _replyToId = null),
                    child: Icon(
                      Icons.close_rounded,
                      size: 16,
                      color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500
                    ),
                  ),
                ],
              ),
            ),
          Row(
            children: [
              Expanded(
                child: ValoraTextField(
                  controller: _controller,
                  hint: 'Add a comment...',
                  maxLines: 5,
                ),
              ),
              const SizedBox(width: ValoraSpacing.sm),
              _isSubmitting
                  ? const Padding(
                      padding: EdgeInsets.all(12.0),
                      child: SizedBox(
                        width: 24,
                        height: 24,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      ),
                    )
                  : IconButton(
                      icon: const Icon(Icons.send_rounded),
                      color: ValoraColors.primary,
                      onPressed: _submit,
                    ),
            ],
          ),
        ],
      ),
    );
  }

  Future<void> _submit() async {
    if (_controller.text.isEmpty) return;

    setState(() => _isSubmitting = true);

    try {
      await context.read<WorkspaceProvider>().addComment(widget.savedListingId, _controller.text, _replyToId);
      _controller.clear();
      setState(() => _replyToId = null);
      widget.onRefresh?.call();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Failed to post comment: $e'),
            backgroundColor: ValoraColors.error,
          ),
        );
      }
    } finally {
      if (mounted) {
        setState(() => _isSubmitting = false);
      }
    }
  }
}
