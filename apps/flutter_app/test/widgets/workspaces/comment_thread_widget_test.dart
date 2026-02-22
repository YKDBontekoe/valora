import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/widgets/workspaces/comment_thread_widget.dart';
import 'package:valora_app/providers/workspace_provider.dart';
import 'package:valora_app/models/comment.dart';
import 'package:valora_app/models/workspace.dart';
import 'package:valora_app/models/saved_listing.dart';
import 'package:valora_app/models/activity_log.dart';

class MockWorkspaceProvider extends ChangeNotifier implements WorkspaceProvider {
  bool addCommentCalled = false;
  String? lastCommentContent;
  String? lastParentId;

  @override
  bool get isLoading => false;
  @override
  String? get error => null;
  @override
  List<Workspace> get workspaces => [];
  @override
  Workspace? get selectedWorkspace => null;
  @override
  List<SavedListing> get savedListings => [];
  @override
  List<WorkspaceMember> get members => [];
  @override
  List<ActivityLog> get activityLogs => [];

  @override
  Future<void> addComment(String savedListingId, String content, String? parentId) async {
    addCommentCalled = true;
    lastCommentContent = content;
    lastParentId = parentId;
    notifyListeners();
  }

  @override
  Future<void> fetchWorkspaces() async {}
  @override
  Future<void> createWorkspace(String name, String? description) async {}
  @override
  Future<void> selectWorkspace(String id) async {}
  @override
  Future<void> inviteMember(String email, WorkspaceRole role) async {}
  @override
  Future<void> saveListing(String listingId, String? notes) async {}
  @override
  Future<List<Comment>> fetchComments(String savedListingId) async => [];
}

void main() {
  testWidgets('CommentThreadWidget renders empty state', (WidgetTester tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: CommentThreadWidget(savedListingId: '1', comments: []),
        ),
      ),
    );

    expect(find.text('No comments yet.'), findsOneWidget);
  });

  testWidgets('CommentThreadWidget renders comments and replies', (WidgetTester tester) async {
    final comments = [
      Comment(
        id: '1',
        userId: 'user1',
        content: 'Hello World',
        createdAt: DateTime.now(),
        replies: [
          Comment(
            id: '2',
            userId: 'user2',
            content: 'Nested reply',
            createdAt: DateTime.now(),
            replies: [],
            reactions: {},
          ),
        ],
        reactions: {},
      ),
    ];

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: CommentThreadWidget(savedListingId: '1', comments: comments),
        ),
      ),
    );

    expect(find.text('Hello World'), findsOneWidget);
    expect(find.text('Nested reply'), findsOneWidget);
  });

  testWidgets('CommentThreadWidget allows adding a comment', (WidgetTester tester) async {
    final mockProvider = MockWorkspaceProvider();

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: ChangeNotifierProvider<WorkspaceProvider>.value(
            value: mockProvider,
            child: const CommentThreadWidget(savedListingId: '1', comments: []),
          ),
        ),
      ),
    );

    await tester.enterText(find.byType(TextField), 'New comment');
    await tester.tap(find.byIcon(Icons.send));
    await tester.pump();

    expect(mockProvider.addCommentCalled, true);
    expect(mockProvider.lastCommentContent, 'New comment');
    expect(mockProvider.lastParentId, null);
  });

  testWidgets('CommentThreadWidget allows replying', (WidgetTester tester) async {
    final mockProvider = MockWorkspaceProvider();
    final comments = [
      Comment(
        id: '100',
        userId: 'user1',
        content: 'Original',
        createdAt: DateTime.now(),
        replies: [],
        reactions: {},
      ),
    ];

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: ChangeNotifierProvider<WorkspaceProvider>.value(
            value: mockProvider,
            child: CommentThreadWidget(savedListingId: '1', comments: comments),
          ),
        ),
      ),
    );

    // Tap reply icon on the comment
    await tester.tap(find.byIcon(Icons.reply));
    await tester.pump();

    expect(find.text('Reply...'), findsOneWidget); // Hint text changes

    await tester.enterText(find.byType(TextField), 'My reply');
    await tester.tap(find.byIcon(Icons.send));
    await tester.pump();

    expect(mockProvider.addCommentCalled, true);
    expect(mockProvider.lastCommentContent, 'My reply');
    expect(mockProvider.lastParentId, '100');
  });
}
