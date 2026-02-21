import 'package:flutter/material.dart';
import '../../models/comment.dart';
import 'package:provider/provider.dart';
import '../../providers/workspace_provider.dart';
import 'package:intl/intl.dart';

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

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Expanded(
          child: widget.comments.isEmpty
          ? const Center(child: Text('No comments yet.'))
          : ListView.builder(
            itemCount: widget.comments.length,
            itemBuilder: (context, index) => _buildComment(widget.comments[index]),
          ),
        ),
        _buildInput(),
      ],
    );
  }

  Widget _buildComment(Comment comment) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4.0),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          ListTile(
            leading: CircleAvatar(child: Text(comment.userId.isNotEmpty ? comment.userId[0].toUpperCase() : '?')),
            title: Text(comment.content),
            subtitle: Text(DateFormat.yMMMd().add_jm().format(comment.createdAt)),
            trailing: IconButton(
              icon: const Icon(Icons.reply),
              onPressed: () => setState(() => _replyToId = comment.id),
            ),
          ),
          if (comment.replies.isNotEmpty)
            Padding(
              padding: const EdgeInsets.only(left: 32.0),
              child: Column(
                children: comment.replies.map((r) => _buildComment(r)).toList(),
              ),
            ),
        ],
      ),
    );
  }

  Widget _buildInput() {
    return Container(
      padding: const EdgeInsets.all(8.0),
      color: Colors.grey[200],
      child: Row(
        children: [
          if (_replyToId != null)
            IconButton(
              icon: const Icon(Icons.close),
              onPressed: () => setState(() => _replyToId = null),
            ),
          Expanded(
            child: TextField(
              controller: _controller,
              decoration: InputDecoration(
                hintText: _replyToId != null ? 'Reply...' : 'Add a comment...',
                border: const OutlineInputBorder(),
              ),
            ),
          ),
          IconButton(
            icon: const Icon(Icons.send),
            onPressed: _submit,
          ),
        ],
      ),
    );
  }

  void _submit() async {
    if (_controller.text.isEmpty) return;
    await context.read<WorkspaceProvider>().addComment(widget.savedListingId, _controller.text, _replyToId);
    _controller.clear();
    setState(() => _replyToId = null);
    widget.onRefresh?.call();
  }
}
