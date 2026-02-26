import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/providers/auth_provider.dart';
import 'package:valora_app/providers/workspace_provider.dart';
import 'package:valora_app/screens/workspace_detail_screen.dart';
import 'package:valora_app/models/workspace.dart';
import 'package:valora_app/models/saved_listing.dart';
import 'package:valora_app/models/activity_log.dart';
import 'package:valora_app/models/comment.dart';

class MockAuthProvider extends ChangeNotifier implements AuthProvider {
  @override
  String? get email => 'owner@example.com';

  @override
  bool get isAuthenticated => true;

  @override
  bool get isLoading => false;

  @override
  String? get token => 'token';

  @override
  Future<void> checkAuth() async {}
  @override
  Future<void> login(String email, String password) async {}
  @override
  Future<void> register(String email, String password, String confirmPassword) async {}
  @override
  Future<void> logout() async {}
  @override
  Future<String?> refreshSession() async => null;
  @override
  Future<void> loginWithGoogle() async {}
}

// Create a simple mock manually instead of using Mockito's generator for this quick test
class MockWorkspaceProvider extends ChangeNotifier implements WorkspaceProvider {
  @override
  bool get isWorkspacesLoading => false;

  @override
  bool get isWorkspaceDetailLoading => false;

  @override
  bool get isDeletingWorkspace => false;

  @override
  String? get error => null;

  @override
  Object? get exception => null;

  @override
  Workspace? get selectedWorkspace => Workspace(
    id: '1',
    name: 'Test Workspace',
    description: 'Desc',
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
  List<Workspace> get workspaces => [];

  @override
  Future<void> selectWorkspace(String id) async {
     // Mock implementation
  }

  @override
  Future<void> fetchWorkspaces() async {}

  @override
  Future<void> createWorkspace(String name, String? description) async {}

  @override
  Future<void> deleteWorkspace(String id) async {}

  @override
  Future<void> inviteMember(String email, WorkspaceRole role) async {}

  @override
  Future<void> removeMember(String memberId) async {}

  @override
  Future<void> leaveWorkspace(String memberId) async {}

  @override
  Future<void> updateWorkspace(String id, String name, String? description) async {}

  @override
  Future<void> saveListing(String listingId, String? notes) async {}

  @override
  Future<void> addComment(String savedListingId, String content, String? parentId) async {}

  @override
  Future<List<Comment>> fetchComments(String savedListingId) async => [];
}

void main() {
  testWidgets('WorkspaceDetailScreen renders correctly', (WidgetTester tester) async {
    final mockProvider = MockWorkspaceProvider();
    final mockAuthProvider = MockAuthProvider();

    await tester.pumpWidget(
      MaterialApp(
        home: MultiProvider(
          providers: [
            ChangeNotifierProvider<WorkspaceProvider>.value(value: mockProvider),
            ChangeNotifierProvider<AuthProvider>.value(value: mockAuthProvider),
          ],
          child: const WorkspaceDetailScreen(workspaceId: '1'),
        ),
      ),
    );

    // Allow post-frame callback
    await tester.pumpAndSettle();

    expect(find.text('Test Workspace'), findsOneWidget);
    expect(find.text('Saved'), findsOneWidget);
    expect(find.text('Members'), findsOneWidget);
    expect(find.text('Activity'), findsOneWidget);
  });
}
