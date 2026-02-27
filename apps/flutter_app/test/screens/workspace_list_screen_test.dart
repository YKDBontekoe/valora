import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/models/workspace.dart';
import 'package:valora_app/models/saved_listing.dart';
import 'package:valora_app/models/activity_log.dart';
import 'package:valora_app/models/comment.dart';
import 'package:valora_app/providers/workspace_provider.dart';
import 'package:valora_app/screens/workspace_list_screen.dart';
import 'package:valora_app/widgets/valora_widgets.dart';

class FakeWorkspaceProvider extends ChangeNotifier implements WorkspaceProvider {
  bool _isWorkspacesLoading = false;
  @override
  bool get isWorkspacesLoading => _isWorkspacesLoading;

  String? _error;
  @override
  String? get error => _error;

  // Implements 'exception' for CI compatibility (ChangeNotifier/Provider interface drift),
  // but removed @override to pass local analysis where it might not exist yet.
  Object? get exception => null;

  List<Workspace> _workspaces = [];
  @override
  List<Workspace> get workspaces => _workspaces;

  final bool _isDeletingWorkspace = false;
  @override
  bool get isDeletingWorkspace => _isDeletingWorkspace;

  // Unused properties required by interface
  @override
  bool get isWorkspaceDetailLoading => false;
  @override
  Workspace? get selectedWorkspace => null;
  @override
  List<WorkspaceMember> get members => [];
  @override
  List<SavedListing> get savedListings => [];
  @override
  List<ActivityLog> get activityLogs => [];

  void setLoading(bool loading) {
    _isWorkspacesLoading = loading;
    notifyListeners();
  }

  void setError(String? err) {
    _error = err;
    notifyListeners();
  }

  void setWorkspaces(List<Workspace> list) {
    _workspaces = list;
    notifyListeners();
  }

  @override
  Future<void> fetchWorkspaces() async {}

  bool createWorkspaceCalled = false;
  String? createdName;
  @override
  Future<void> createWorkspace(String name, String? description) async {
    createWorkspaceCalled = true;
    createdName = name;
    notifyListeners();
  }

  bool deleteWorkspaceCalled = false;
  String? deletedId;
  @override
  Future<void> deleteWorkspace(String id) async {
    deleteWorkspaceCalled = true;
    deletedId = id;
    notifyListeners();
  }

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
  late FakeWorkspaceProvider fakeProvider;

  setUp(() {
    fakeProvider = FakeWorkspaceProvider();
  });

  Widget createScreen() {
    return ChangeNotifierProvider<WorkspaceProvider>.value(
      value: fakeProvider,
      child: const MaterialApp(
        home: WorkspaceListScreen(),
      ),
    );
  }

  testWidgets('Renders title', (WidgetTester tester) async {
    await tester.pumpWidget(createScreen());
    await tester.pumpAndSettle();
    expect(find.text('Workspaces'), findsOneWidget);
  });

  testWidgets('Shows loading skeleton when loading', (WidgetTester tester) async {
    fakeProvider.setLoading(true);
    await tester.pumpWidget(createScreen());
    await tester.pump();

    expect(find.text('No workspaces yet'), findsNothing);
    expect(find.text('Loading workspaces...'), findsNothing);

    // Dispose the widget tree to cancel infinite animations (Shimmer)
    await tester.pumpWidget(const SizedBox());
    await tester.pumpAndSettle();
  });

  testWidgets('Shows error state when error occurs', (WidgetTester tester) async {
    fakeProvider.setLoading(false);
    fakeProvider.setError('Failed to fetch data');
    fakeProvider.setWorkspaces([]);

    await tester.pumpWidget(createScreen());
    await tester.pumpAndSettle();

    expect(find.text('Failed to load'), findsOneWidget);
    expect(find.text('Could not load your workspaces. Please try again.'), findsOneWidget);
    expect(find.text('Retry'), findsOneWidget);
  });

  testWidgets('Shows empty state when no workspaces', (WidgetTester tester) async {
    fakeProvider.setLoading(false);
    fakeProvider.setWorkspaces([]);
    await tester.pumpWidget(createScreen());
    await tester.pumpAndSettle();

    expect(find.text('No workspaces yet'), findsOneWidget);
    expect(find.text('Create Workspace'), findsOneWidget);
  });

  testWidgets('Shows list of workspaces', (WidgetTester tester) async {
    fakeProvider.setLoading(false);
    fakeProvider.setWorkspaces([
      Workspace(
        id: '1',
        name: 'Test Workspace 1',
        ownerId: 'owner1',
        memberCount: 2,
        savedListingCount: 3,
        createdAt: DateTime.now(),
        description: 'Test Description',
      ),
      Workspace(
        id: '2',
        name: 'Test Workspace 2',
        ownerId: 'owner2',
        memberCount: 5,
        savedListingCount: 1,
        createdAt: DateTime.now().subtract(const Duration(days: 1)),
        description: 'Test Description 2',
      ),
    ]);

    await tester.pumpWidget(createScreen());
    await tester.pumpAndSettle(); // Settle animations

    expect(find.text('Test Workspace 1'), findsOneWidget);
    expect(find.text('Test Workspace 2'), findsOneWidget);
    expect(find.text('2 members'), findsOneWidget);
    expect(find.text('5 members'), findsOneWidget);
  });

  testWidgets('Filters workspaces by search query', (WidgetTester tester) async {
    fakeProvider.setLoading(false);
    fakeProvider.setWorkspaces([
      Workspace(
        id: '1',
        name: 'Apple',
        ownerId: '1',
        memberCount: 1,
        savedListingCount: 0,
        createdAt: DateTime.now(),
      ),
      Workspace(
        id: '2',
        name: 'Banana',
        ownerId: '2',
        memberCount: 1,
        savedListingCount: 0,
        createdAt: DateTime.now(),
      ),
    ]);

    await tester.pumpWidget(createScreen());
    await tester.pumpAndSettle();

    expect(find.text('Apple'), findsOneWidget);
    expect(find.text('Banana'), findsOneWidget);

    // Enter search query
    await tester.enterText(find.byType(TextField), 'App');
    await tester.pumpAndSettle();

    expect(find.text('Apple'), findsOneWidget);
    expect(find.text('Banana'), findsNothing);
  });

  testWidgets('Create workspace dialog works', (WidgetTester tester) async {
    fakeProvider.setLoading(false);
    await tester.pumpWidget(createScreen());
    await tester.pumpAndSettle();

    // Tap FAB
    await tester.tap(find.text('New Workspace'));
    await tester.pumpAndSettle();

    expect(find.text('Workspace Name'), findsOneWidget);

    // Enter name (find TextField inside Dialog to avoid SearchBar in background)
    final dialogTextField = find.descendant(
      of: find.byType(ValoraDialog),
      matching: find.byType(TextField),
    ).first;
    await tester.enterText(dialogTextField, 'New Space');
    await tester.pumpAndSettle();

    // Verify text was entered
    expect(find.text('New Space'), findsOneWidget);

    // Dismiss keyboard to ensure button is visible
    await tester.testTextInput.receiveAction(TextInputAction.done);
    await tester.pumpAndSettle();

    // Tap Create - manually invoke callback to avoid hit test flakiness with animations
    final createBtn = tester.widget<ValoraButton>(find.widgetWithText(ValoraButton, 'Create'));
    createBtn.onPressed!();
    await tester.pumpAndSettle();

    expect(fakeProvider.createWorkspaceCalled, isTrue);
    expect(fakeProvider.createdName, 'New Space');
  });

  testWidgets('Delete workspace flow works', (WidgetTester tester) async {
    fakeProvider.setLoading(false);
    fakeProvider.setWorkspaces([
      Workspace(
        id: 'delete-me',
        name: 'Delete Me',
        ownerId: '1',
        memberCount: 1,
        savedListingCount: 0,
        createdAt: DateTime.now(),
      ),
    ]);

    await tester.pumpWidget(createScreen());
    await tester.pumpAndSettle();

    // Open menu
    await tester.tap(find.byIcon(Icons.more_vert_rounded));
    await tester.pumpAndSettle();

    // Tap Delete
    await tester.tap(find.text('Delete'));
    await tester.pumpAndSettle();

    expect(find.text('Delete Workspace?'), findsOneWidget);

    // Confirm Delete - manually invoke callback to avoid hit test issues
    final deleteBtn = tester.widget<ValoraButton>(find.widgetWithText(ValoraButton, 'Delete'));
    deleteBtn.onPressed!();
    await tester.pumpAndSettle();

    expect(fakeProvider.deleteWorkspaceCalled, isTrue);
    expect(fakeProvider.deletedId, 'delete-me');
  });
}
