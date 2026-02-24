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

  static final DateTime _fixedNow = DateTime(2024, 1, 1);
  List<Workspace> _workspaces = [
    Workspace(
      id: '1',
      name: 'Test Workspace',
      description: 'Desc',
      ownerId: 'owner',
      createdAt: _fixedNow,
      memberCount: 1,
      savedListingCount: 0,
    ),
    Workspace(
      id: '2',
      name: 'Alpha Workspace',
      description: 'A description',
      ownerId: 'owner',
      createdAt: _fixedNow.subtract(const Duration(days: 1)),
      memberCount: 5,
      savedListingCount: 2,
    )
  ];

  @override
  List<Workspace> get workspaces => _workspaces;

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

  testWidgets('WorkspaceListScreen filters list by search query', (WidgetTester tester) async {
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

    // Enable search
    await tester.tap(find.byIcon(Icons.search_rounded));
    await tester.pumpAndSettle();

    // Enter query "Alpha"
    await tester.enterText(find.byType(TextField), 'Alpha');
    await tester.pumpAndSettle();

    expect(find.text('Alpha Workspace'), findsOneWidget);
    expect(find.text('Test Workspace'), findsNothing);
  });

  testWidgets('WorkspaceListScreen sorts list', (WidgetTester tester) async {
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

    // Default sort is Newest First (Test Workspace is newer)
    final firstItemFinder = find.descendant(of: find.byType(ListView), matching: find.text('Test Workspace'));
    expect(firstItemFinder, findsOneWidget);

    // Change sort to Name (Alpha should be first)
    await tester.tap(find.byIcon(Icons.sort_rounded));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Name (A-Z)'));
    await tester.pumpAndSettle();

    // Verify Alpha comes first (simplest way is to verify it exists, order verification in listview can be tricky but finders return in tree order)
    // We can check the first item in the list
    final listFinder = find.byType(ListView);
    final firstCard = find.descendant(of: listFinder, matching: find.text('Alpha Workspace')).first;
    expect(firstCard, findsOneWidget);

    // To ensure order, we check their relative Y positions
    final alphaY = tester.getTopLeft(find.text('Alpha Workspace')).dy;
    final testY = tester.getTopLeft(find.text('Test Workspace')).dy;
    expect(alphaY < testY, isTrue);
  });
}
