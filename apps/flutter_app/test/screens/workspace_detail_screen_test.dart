import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/mockito.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/models/workspace.dart';
import 'package:valora_app/models/saved_listing.dart';
import 'package:valora_app/models/activity_log.dart';
import 'package:valora_app/providers/workspace_provider.dart';
import 'package:valora_app/screens/workspace_detail_screen.dart';

// Mock Provider
class MockWorkspaceProvider extends Mock implements WorkspaceProvider {
  @override
  bool get isWorkspaceDetailLoading => false;
  @override
  String? get error => null;
  @override
  Workspace? get selectedWorkspace => Workspace(
    id: '1',
    name: 'Test Workspace',
    ownerId: '1',
    memberCount: 1,
    savedListingCount: 0,
    createdAt: DateTime.now(),
  );
  @override
  List<SavedListing> get savedListings => [];
  @override
  List<WorkspaceMember> get members => [];
  @override
  List<ActivityLog> get activityLogs => [];

  @override
  Future<void> selectWorkspace(String id) async {}
}

void main() {
  late MockWorkspaceProvider mockProvider;

  setUp(() {
    mockProvider = MockWorkspaceProvider();
  });

  Widget createScreen() {
    return ChangeNotifierProvider<WorkspaceProvider>.value(
      value: mockProvider,
      child: const MaterialApp(
        home: WorkspaceDetailScreen(workspaceId: '1'),
      ),
    );
  }

  testWidgets('WorkspaceDetailScreen renders tabs', (WidgetTester tester) async {
    await tester.pumpWidget(createScreen());
    await tester.pumpAndSettle();

    expect(find.text('Saved'), findsOneWidget);
    expect(find.text('Members'), findsOneWidget);
    expect(find.text('Activity'), findsOneWidget);
  });

  testWidgets('WorkspaceDetailScreen shows empty state for saved listings', (WidgetTester tester) async {
    await tester.pumpWidget(createScreen());
    await tester.pumpAndSettle();

    expect(find.text('No saved listings'), findsOneWidget);
  });
}
