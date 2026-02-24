import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/providers/workspace_provider.dart';
import 'package:valora_app/screens/workspace_list_screen.dart';
import 'package:valora_app/models/workspace.dart';
import 'package:valora_app/models/saved_listing.dart';
import 'package:valora_app/models/activity_log.dart';
import 'package:valora_app/models/comment.dart';

class MockWorkspaceProvider extends ChangeNotifier implements WorkspaceProvider {
  @override
  bool get isWorkspacesLoading => false;

  @override
  bool get isWorkspaceDetailLoading => false;

  @override
  String? get error => null;

  @override
  List<Workspace> get workspaces => [
    Workspace(
      id: '1',
      name: 'Test Workspace',
      description: 'Desc',
      ownerId: 'owner',
      createdAt: DateTime.now(),
      memberCount: 1,
      savedListingCount: 0,
    )
  ];

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

  @override
  Future<void> updateWorkspace(String id, String name, String? description) async {}

  @override
  Future<void> deleteWorkspace(String id) async {}
}

void main() {
  testWidgets('WorkspaceListScreen renders list', (WidgetTester tester) async {
    final mockProvider = MockWorkspaceProvider();

    await tester.pumpWidget(
      MaterialApp(
        home: ChangeNotifierProvider<WorkspaceProvider>.value(
          value: mockProvider,
          child: const WorkspaceListScreen(),
        ),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('Workspaces'), findsOneWidget);
    expect(find.text('Test Workspace'), findsOneWidget);
    expect(find.byType(FloatingActionButton), findsOneWidget);
  });

  testWidgets('WorkspaceListScreen shows create dialog', (WidgetTester tester) async {
    final mockProvider = MockWorkspaceProvider();

    await tester.pumpWidget(
      MaterialApp(
        home: ChangeNotifierProvider<WorkspaceProvider>.value(
          value: mockProvider,
          child: const WorkspaceListScreen(),
        ),
      ),
    );

    await tester.pumpAndSettle();
    await tester.tap(find.byType(FloatingActionButton));
    await tester.pumpAndSettle();

    expect(find.text('New Workspace'), findsNWidgets(2)); // FAB + Dialog title
    expect(find.text('Workspace Name'), findsOneWidget);
    expect(find.text('Create'), findsOneWidget);
  });
}
