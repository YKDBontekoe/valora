import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/providers/workspace_provider.dart';
import 'package:valora_app/widgets/workspaces/share_workspace_dialog.dart';
import 'package:valora_app/models/workspace.dart';
import 'package:valora_app/models/saved_listing.dart';
import 'package:valora_app/models/activity_log.dart';
import 'package:valora_app/models/comment.dart';

class MockWorkspaceProvider extends ChangeNotifier implements WorkspaceProvider {
  bool inviteCalled = false;

  @override
  bool get isWorkspacesLoading => false;
  @override
  bool get isWorkspaceDetailLoading => false;

  @override
  bool get isDeletingWorkspace => false;

  @override
  String? get error => null;
  @override
  List<Workspace> get workspaces => [];
  @override
  Workspace? get selectedWorkspace => Workspace(
    id: '1',
    name: 'WS',
    ownerId: 'owner',
    createdAt: DateTime.now(),
    memberCount: 1,
    savedListingCount: 0,
  );
  @override
  List<SavedListing> get savedListings => [];
  @override
  List<WorkspaceMember> get members => [];
  @override
  List<ActivityLog> get activityLogs => [];

  @override
  Future<void> inviteMember(String email, WorkspaceRole role) async {
    inviteCalled = true;
    notifyListeners();
  }

  @override
  Future<void> fetchWorkspaces() async {}
  @override
  Future<void> createWorkspace(String name, String? description) async {}
  @override
  Future<void> deleteWorkspace(String id) async {}
  @override
  Future<void> selectWorkspace(String id) async {}
  @override
  Future<void> saveListing(String listingId, String? notes) async {}
  @override
  Future<void> addComment(String savedListingId, String content, String? parentId) async {}
  @override
  Future<List<Comment>> fetchComments(String savedListingId) async => [];
}

void main() {
  testWidgets('ShareWorkspaceDialog submits invite', (WidgetTester tester) async {
    final mockProvider = MockWorkspaceProvider();

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Builder(
            builder: (context) => ElevatedButton(
              onPressed: () => showDialog(
                context: context,
                builder: (_) => ChangeNotifierProvider<WorkspaceProvider>.value(
                  value: mockProvider,
                  child: const ShareWorkspaceDialog(),
                ),
              ),
              child: const Text('Open'),
            ),
          ),
        ),
      ),
    );

    await tester.tap(find.text('Open'));
    await tester.pumpAndSettle();

    final textField = find.byType(TextField);
    await tester.enterText(textField, 'test@example.com');
    await tester.pump();

    final inviteButton = find.text('Invite');
    await tester.tap(inviteButton);

    await tester.pump();
    await tester.pump(Duration.zero);

    expect(mockProvider.inviteCalled, true);

    await tester.pumpAndSettle();
  });
}
