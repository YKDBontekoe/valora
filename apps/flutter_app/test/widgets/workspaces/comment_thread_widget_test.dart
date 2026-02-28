import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/models/comment.dart';
import 'package:valora_app/providers/workspace_provider.dart';
import 'package:valora_app/widgets/workspaces/comment_thread_widget.dart';
import 'package:valora_app/widgets/valora_widgets.dart';

class MockWorkspaceProvider extends ChangeNotifier implements WorkspaceProvider {
  String? addedCommentContent;
  String? addedCommentReplyToId;
  bool shouldThrow = false;

  @override
  Future<void> addComment(String savedListingId, String content, String? parentId) async {
    if (shouldThrow) throw Exception('API Error');
    addedCommentContent = content;
    addedCommentReplyToId = parentId;
  }

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

void main() {
  late MockWorkspaceProvider mockProvider;

  setUp(() {
    mockProvider = MockWorkspaceProvider();
  });

  Widget createWidgetUnderTest(List<Comment> comments) {
    return MaterialApp(
      home: Scaffold(
        body: ChangeNotifierProvider<WorkspaceProvider>.value(
          value: mockProvider,
          child: CommentThreadWidget(
            savedListingId: 'listing1',
            comments: comments,
          ),
        ),
      ),
    );
  }

  testWidgets('renders empty state when no comments', (tester) async {
    await tester.pumpWidget(createWidgetUnderTest([]));
    await tester.pumpAndSettle();

    expect(find.byType(ValoraEmptyState), findsOneWidget);
    expect(find.text('No comments yet'), findsOneWidget);

    await tester.pumpWidget(const SizedBox());
    await tester.pumpAndSettle();
  });

  testWidgets('renders comments and replies', (tester) async {
    final List<Comment> comments = [
      Comment(
        id: '1',
        userId: 'User 1',
        content: 'This is a comment',
        createdAt: DateTime(2023, 1, 1, 10, 0),
        reactions: {},
        replies: [
          Comment(
            id: '2',
            userId: 'User 2',
            content: 'This is a reply',
            createdAt: DateTime(2023, 1, 1, 11, 0),
            parentId: '1',
            reactions: {},
            replies: [],
          ),
        ],
      ),
    ];

    await tester.pumpWidget(createWidgetUnderTest(comments));
    await tester.pumpAndSettle();

    expect(find.byType(ValoraCard), findsNWidgets(2));
    expect(find.text('This is a comment'), findsOneWidget);
    expect(find.text('This is a reply'), findsOneWidget);

    await tester.pumpWidget(const SizedBox());
    await tester.pumpAndSettle();
  });

  testWidgets('can reply to a comment', (tester) async {
    final List<Comment> comments = [
      Comment(
        id: '1',
        userId: 'User 1',
        content: 'This is a comment',
        createdAt: DateTime(2023, 1, 1, 10, 0),
        reactions: {},
        replies: [],
      ),
    ];

    await tester.pumpWidget(createWidgetUnderTest(comments));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Reply'));
    await tester.pumpAndSettle();

    expect(find.text('Replying to comment'), findsOneWidget);

    final textField = find.byType(ValoraTextField);
    await tester.enterText(textField, 'My new reply');
    await tester.pumpAndSettle();

    await tester.tap(find.byIcon(Icons.send_rounded));
    await tester.pumpAndSettle();

    expect(mockProvider.addedCommentContent, 'My new reply');
    expect(mockProvider.addedCommentReplyToId, '1');
    expect(find.text('Replying to comment'), findsNothing); // Should be cleared

    await tester.pumpWidget(const SizedBox());
    await tester.pumpAndSettle();
  });

  testWidgets('submitting comment handles failure', (tester) async {
    mockProvider.shouldThrow = true;

    await tester.pumpWidget(createWidgetUnderTest([]));
    await tester.pumpAndSettle();

    final textField = find.byType(ValoraTextField);
    await tester.enterText(textField, 'Failed comment');
    await tester.pumpAndSettle();

    await tester.tap(find.byIcon(Icons.send_rounded));
    await tester.pumpAndSettle();

    expect(find.textContaining('Failed to post comment: Exception: API Error'), findsOneWidget);

    await tester.pumpWidget(const SizedBox());
    await tester.pumpAndSettle();
  });
}
