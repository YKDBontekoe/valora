import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/providers/workspace_provider.dart';
import 'package:valora_app/screens/workspace_detail_screen.dart';
import 'package:valora_app/widgets/valora_widgets.dart'; // Import for ValoraTextField
import 'package:valora_app/models/workspace.dart';
import 'package:valora_app/models/saved_listing.dart';
import 'package:valora_app/models/activity_log.dart';
import 'package:valora_app/models/comment.dart';

// Create a simple mock manually instead of using Mockito's generator for this quick test
class MockWorkspaceProvider extends ChangeNotifier implements WorkspaceProvider {
  @override
  bool get isWorkspacesLoading => false;

  @override
  bool get isWorkspaceDetailLoading => false;

  @override
  String? get error => null;

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
  Future<void> inviteMember(String email, WorkspaceRole role) async {}

  @override
  Future<void> saveListing(String listingId, String? notes) async {}

  @override
  Future<void> addComment(String savedListingId, String content, String? parentId) async {}

  @override
  Future<List<Comment>> fetchComments(String savedListingId) async => [];

  bool updateCalled = false;
  bool deleteCalled = false;

  @override
  Future<void> updateWorkspace(String id, String name, String? description) async {
    updateCalled = true;
  }

  @override
  Future<void> deleteWorkspace(String id) async {
    deleteCalled = true;
  }
}

void main() {
  testWidgets('WorkspaceDetailScreen renders correctly', (WidgetTester tester) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final mockProvider = MockWorkspaceProvider();

    await tester.pumpWidget(
      MaterialApp(
        home: ChangeNotifierProvider<WorkspaceProvider>.value(
          value: mockProvider,
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

  testWidgets('WorkspaceDetailScreen shows edit dialog and calls update', (WidgetTester tester) async {
    // Increase size to avoid RenderFlex overflows in dialogs
    tester.view.physicalSize = const Size(2400, 2400);
    tester.view.devicePixelRatio = 3.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final mockProvider = MockWorkspaceProvider();

    await tester.pumpWidget(
      MaterialApp(
        home: ChangeNotifierProvider<WorkspaceProvider>.value(
          value: mockProvider,
          child: const WorkspaceDetailScreen(workspaceId: '1'),
        ),
      ),
    );

    await tester.pumpAndSettle();

    // Open menu
    await tester.tap(find.byIcon(Icons.more_vert));
    await tester.pumpAndSettle();

    // Tap Edit
    await tester.tap(find.text('Edit Workspace'));
    await tester.pumpAndSettle();

    expect(find.text('Edit Workspace'), findsOneWidget); // Dialog title

    // Find the TextField associated with the "Workspace Name" ValoraTextField
    final nameFieldFinder = find.descendant(
      of: find.widgetWithText(ValoraTextField, 'Workspace Name'),
      matching: find.byType(TextField),
    );

    // Enter new name
    await tester.enterText(nameFieldFinder, 'New Name');
    await tester.tap(find.text('Save'));
    await tester.pumpAndSettle();

    expect(mockProvider.updateCalled, isTrue);
  });

  testWidgets('WorkspaceDetailScreen shows delete confirmation and calls delete', (WidgetTester tester) async {
    // Increase size and reduce text scale to avoid RenderFlex overflows in dialogs
    tester.view.physicalSize = const Size(2400, 2400);
    tester.view.devicePixelRatio = 3.0;
    tester.platformDispatcher.textScaleFactorTestValue = 0.5;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    addTearDown(tester.platformDispatcher.clearTextScaleFactorTestValue);

    final mockProvider = MockWorkspaceProvider();

    await tester.pumpWidget(
      MaterialApp(
        home: ChangeNotifierProvider<WorkspaceProvider>.value(
          value: mockProvider,
          child: const WorkspaceDetailScreen(workspaceId: '1'),
        ),
      ),
    );

    await tester.pumpAndSettle();

    // Open menu
    await tester.tap(find.byIcon(Icons.more_vert));
    await tester.pumpAndSettle();

    // Tap Delete
    await tester.tap(find.text('Delete Workspace'));
    await tester.pumpAndSettle();

    expect(find.text('Delete Workspace?'), findsOneWidget); // Confirmation Dialog title

    // Tap Confirm Delete
    await tester.tap(find.text('Delete')); // ValoraButton renders text
    await tester.pumpAndSettle();

    expect(mockProvider.deleteCalled, isTrue);
  });
}
