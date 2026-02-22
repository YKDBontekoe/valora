import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/widgets/workspaces/member_management_widget.dart';
import 'package:valora_app/models/workspace.dart';
import 'package:valora_app/widgets/workspaces/share_workspace_dialog.dart';
import 'package:valora_app/providers/workspace_provider.dart';
import 'package:valora_app/models/saved_listing.dart';
import 'package:valora_app/models/activity_log.dart';
import 'package:valora_app/models/comment.dart';

class MockWorkspaceProvider extends ChangeNotifier implements WorkspaceProvider {
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
  Future<void> addComment(String savedListingId, String content, String? parentId) async {}
  @override
  Future<List<Comment>> fetchComments(String savedListingId) async => [];
}

void main() {
  testWidgets('MemberManagementWidget renders members', (WidgetTester tester) async {
    final members = [
      WorkspaceMember(id: '1', email: 'user1@example.com', role: WorkspaceRole.owner, isPending: false),
      WorkspaceMember(id: '2', email: 'user2@example.com', role: WorkspaceRole.viewer, isPending: true),
    ];

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: MemberManagementWidget(members: members, canInvite: false),
        ),
      ),
    );

    expect(find.text('user1@example.com'), findsOneWidget);
    expect(find.text('OWNER'), findsOneWidget);
    expect(find.text('user2@example.com'), findsOneWidget);
    expect(find.text('VIEWER'), findsOneWidget);
    expect(find.text('Pending'), findsOneWidget);
    expect(find.text('Invite Member'), findsNothing);
  });

  testWidgets('MemberManagementWidget shows invite button and dialog', (WidgetTester tester) async {
    final mockProvider = MockWorkspaceProvider();

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: ChangeNotifierProvider<WorkspaceProvider>.value(
            value: mockProvider,
            child: const MemberManagementWidget(members: [], canInvite: true),
          ),
        ),
      ),
    );

    expect(find.text('Invite Member'), findsOneWidget);
    await tester.tap(find.text('Invite Member'));
    await tester.pumpAndSettle();

    expect(find.byType(ShareWorkspaceDialog), findsOneWidget);
  });
}
