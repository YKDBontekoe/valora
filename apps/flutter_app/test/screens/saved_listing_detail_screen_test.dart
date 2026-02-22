import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:flutter_animate/flutter_animate.dart'; // Import flutter_animate
import 'package:valora_app/providers/workspace_provider.dart';
import 'package:valora_app/screens/saved_listing_detail_screen.dart';
import 'package:valora_app/models/saved_listing.dart';
import 'package:valora_app/models/comment.dart';
import 'package:valora_app/models/workspace.dart';
import 'package:valora_app/models/activity_log.dart';
import 'dart:io';

class MockWorkspaceProvider extends ChangeNotifier implements WorkspaceProvider {
  final List<Comment> mockComments;
  bool shouldFailFetchComments;

  MockWorkspaceProvider({
    this.mockComments = const [],
    this.shouldFailFetchComments = false,
  });

  @override
  bool get isLoading => false;

  @override
  String? get error => null;

  @override
  Workspace? get selectedWorkspace => null;

  @override
  List<SavedListing> get savedListings => [];

  @override
  List<WorkspaceMember> get members => [];

  @override
  List<ActivityLog> get activityLogs => [];

  @override
  List<Workspace> get workspaces => [];

  @override
  Future<void> selectWorkspace(String id) async {}

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
  Future<List<Comment>> fetchComments(String savedListingId) async {
    if (shouldFailFetchComments) {
      throw Exception('Failed to fetch comments');
    }
    return mockComments;
  }
}

void main() {
  setUpAll(() {
    HttpOverrides.global = null;
    // Set default animation duration to zero to effectively disable animations in tests
    Animate.defaultDuration = Duration.zero; 
  });

  final testListing = SavedListing(
    id: 'sl1',
    listingId: 'l1',
    listing: ListingSummary(
      id: 'l1',
      address: 'Test Address 123',
      city: 'Test City',
      price: 500000,
      bedrooms: 3,
      livingAreaM2: 120,
    ),
    addedByUserId: 'u1',
    notes: 'Some important notes about this property.',
    addedAt: DateTime.now(),
    commentCount: 0,
  );

  testWidgets('SavedListingDetailScreen displays listing info and notes', (WidgetTester tester) async {
    tester.view.physicalSize = const Size(1200, 1600);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() => tester.view.resetPhysicalSize());

    final mockProvider = MockWorkspaceProvider();

    await tester.pumpWidget(
      MaterialApp(
        home: ChangeNotifierProvider<WorkspaceProvider>.value(
          value: mockProvider,
          child: SavedListingDetailScreen(savedListing: testListing),
        ),
      ),
    );

    await tester.pumpAndSettle();
    
    expect(find.text('Test Address 123'), findsAtLeast(1));
    expect(find.text('Test City'), findsOneWidget);
    expect(find.textContaining('500.000'), findsOneWidget);
    expect(find.text('3 bedrooms'), findsOneWidget);
    expect(find.text('120 m²'), findsOneWidget);
    expect(find.text('Notes'), findsOneWidget);
    expect(find.text('Some important notes about this property.'), findsOneWidget);
  });

  testWidgets('SavedListingDetailScreen displays comments when they are loaded', (WidgetTester tester) async {
    tester.view.physicalSize = const Size(1200, 1600);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() => tester.view.resetPhysicalSize());

    final List<Comment> mockComments = [
      Comment(
        id: 'c1',
        userId: 'u1',
        content: 'Great property!',
        createdAt: DateTime.now(),
        replies: [],
        reactions: {},
      ),
    ];
    final mockProvider = MockWorkspaceProvider(mockComments: mockComments);

    await tester.pumpWidget(
      MaterialApp(
        home: ChangeNotifierProvider<WorkspaceProvider>.value(
          value: mockProvider,
          child: SavedListingDetailScreen(savedListing: testListing),
        ),
      ),
    );

    expect(find.textContaining('Loading comments'), findsOneWidget);

    await tester.pumpAndSettle();

    expect(find.text('Great property!'), findsOneWidget);
    expect(find.text('U'), findsOneWidget);
  });

  testWidgets('SavedListingDetailScreen displays empty state when no comments', (WidgetTester tester) async {
    tester.view.physicalSize = const Size(1200, 1600);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() => tester.view.resetPhysicalSize());

    final mockProvider = MockWorkspaceProvider(mockComments: []);

    await tester.pumpWidget(
      MaterialApp(
        home: ChangeNotifierProvider<WorkspaceProvider>.value(
          value: mockProvider,
          child: SavedListingDetailScreen(savedListing: testListing),
        ),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('No comments yet'), findsOneWidget);
  });

  testWidgets('SavedListingDetailScreen displays error state when comments fail to load', (WidgetTester tester) async {
    tester.view.physicalSize = const Size(1200, 1600);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(() => tester.view.resetPhysicalSize());

    final mockProvider = MockWorkspaceProvider(shouldFailFetchComments: true);

    await tester.pumpWidget(
      MaterialApp(
        home: ChangeNotifierProvider<WorkspaceProvider>.value(
          value: mockProvider,
          child: SavedListingDetailScreen(savedListing: testListing),
        ),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('Failed to load comments'), findsOneWidget);
    expect(find.text('Retry'), findsOneWidget);

    mockProvider.shouldFailFetchComments = false;
    await tester.tap(find.text('Retry'));
    await tester.pumpAndSettle();

    expect(find.text('No comments yet'), findsOneWidget);
  });
}
