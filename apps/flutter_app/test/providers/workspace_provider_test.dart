import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/mockito.dart';
import 'package:mockito/annotations.dart';
import 'package:valora_app/providers/workspace_provider.dart';
import 'package:valora_app/repositories/workspace_repository.dart';
import 'package:valora_app/models/workspace.dart';
import 'package:valora_app/models/saved_property.dart';
import 'package:valora_app/models/comment.dart';

@GenerateMocks([WorkspaceRepository])
import 'workspace_provider_test.mocks.dart';

void main() {
  late WorkspaceProvider provider;
  late MockWorkspaceRepository mockRepository;

  setUp(() {
    mockRepository = MockWorkspaceRepository();
    provider = WorkspaceProvider(mockRepository);
  });

  group('WorkspaceProvider', () {
    test('fetchWorkspaces populates list on success and implements caching', () async {
      final ws1 = Workspace(
        id: '1',
        name: 'Test',
        ownerId: 'user1',
        createdAt: DateTime.now(),
        memberCount: 1,
        savedListingCount: 0,
      );
      when(mockRepository.fetchWorkspaces()).thenAnswer((_) async => [ws1]);

      // First fetch
      await provider.fetchWorkspaces();

      expect(provider.workspaces.length, 1);
      expect(provider.workspaces.first.name, 'Test');
      expect(provider.isWorkspacesLoading, false);
      expect(provider.error, null);

      // Second fetch (should use caching logic)
      final ws2 = Workspace(
        id: '2',
        name: 'Test 2',
        ownerId: 'user1',
        createdAt: DateTime.now(),
        memberCount: 1,
        savedListingCount: 0,
      );
      when(mockRepository.fetchWorkspaces()).thenAnswer((_) async => [ws1, ws2]);

      final future = provider.fetchWorkspaces();
      // Should remain false because we already have data
      expect(provider.isWorkspacesLoading, false);

      await future;

      expect(provider.workspaces.length, 2);
      expect(provider.isWorkspacesLoading, false);
    });

    test('fetchWorkspaces handles error', () async {
      when(mockRepository.fetchWorkspaces()).thenThrow(Exception('Network error'));

      await provider.fetchWorkspaces();

      expect(provider.workspaces.isEmpty, true);
      expect(provider.error, contains('Network error'));
      expect(provider.isWorkspacesLoading, false);
    });

    test('createWorkspace adds new workspace', () async {
      final newWs = Workspace(
        id: '2',
        name: 'New',
        ownerId: 'user1',
        createdAt: DateTime.now(),
        memberCount: 1,
        savedListingCount: 0,
      );

      when(mockRepository.createWorkspace(any, any)).thenAnswer((_) async => newWs);

      await provider.createWorkspace('New', 'Desc');

      expect(provider.workspaces.length, 1);
      expect(provider.workspaces.first.id, '2');
    });

    test('selectWorkspace fetches details and implements caching', () async {
      when(mockRepository.getWorkspace('1')).thenAnswer((_) async =>
        Workspace(
          id: '1',
          name: 'WS',
          ownerId: 'user1',
          createdAt: DateTime.now(),
          memberCount: 1,
          savedListingCount: 0,
        )
      );
      when(mockRepository.getWorkspaceMembers('1')).thenAnswer((_) async => []);
      when(mockRepository.getWorkspaceProperties('1')).thenAnswer((_) async => []);
      when(mockRepository.getWorkspaceActivity('1')).thenAnswer((_) async => []);

      // First select
      await provider.selectWorkspace('1');

      expect(provider.selectedWorkspace?.id, '1');
      expect(provider.isWorkspaceDetailLoading, false);

      // Second select of the SAME workspace
      final future = provider.selectWorkspace('1');
      // Should remain false because it's already selected
      expect(provider.isWorkspaceDetailLoading, false);

      await future;

      expect(provider.selectedWorkspace?.id, '1');
      expect(provider.isWorkspaceDetailLoading, false);
    });

    test('inviteMember calls API and refreshes members', () async {
      // Setup selected workspace
      when(mockRepository.getWorkspace('1')).thenAnswer((_) async =>
        Workspace(
          id: '1',
          name: 'WS',
          ownerId: 'user1',
          createdAt: DateTime.now(),
          memberCount: 1,
          savedListingCount: 0,
        )
      );
      when(mockRepository.getWorkspaceMembers('1')).thenAnswer((_) async => []);
      when(mockRepository.getWorkspaceProperties('1')).thenAnswer((_) async => []);
      when(mockRepository.getWorkspaceActivity('1')).thenAnswer((_) async => []);

      await provider.selectWorkspace('1');

      // Setup invite call
      when(mockRepository.inviteMember(any, any, any)).thenAnswer((_) async {});
      when(mockRepository.getWorkspaceMembers('1')).thenAnswer((_) async => [
        WorkspaceMember(
          id: 'wm1',
          userId: 'u2',
          role: WorkspaceRole.viewer,
          email: 'test@example.com',
          isPending: false,
          joinedAt: DateTime.now(),
        )
      ]);

      await provider.inviteMember('test@example.com', WorkspaceRole.viewer);

      expect(provider.members.length, 1);
      expect(provider.members.first.email, 'test@example.com');
    });

    test('saveProperty calls API and refreshes listings', () async {
      // Setup selected workspace
      when(mockRepository.getWorkspace('1')).thenAnswer((_) async =>
        Workspace(
          id: '1',
          name: 'WS',
          ownerId: 'user1',
          createdAt: DateTime.now(),
          memberCount: 1,
          savedListingCount: 0,
        )
      );
      when(mockRepository.getWorkspaceMembers('1')).thenAnswer((_) async => []);
      when(mockRepository.getWorkspaceProperties('1')).thenAnswer((_) async => []);
      when(mockRepository.getWorkspaceActivity('1')).thenAnswer((_) async => []);
      await provider.selectWorkspace('1');

      // Setup save call
      when(mockRepository.saveProperty(any, any, any)).thenAnswer((_) async {});
      when(mockRepository.getWorkspaceProperties('1')).thenAnswer((_) async => [
        SavedProperty(
          id: 'sl1',
          propertyId: 'l1',
          addedByUserId: 'u1',
          addedAt: DateTime.now(),
          commentCount: 0,
          property: PropertySummary(id: 'l1', address: '123 St')
        )
      ]);

      await provider.saveProperty('l1', 'notes');

      expect(provider.savedProperties.length, 1);
      expect(provider.savedProperties.first.propertyId, 'l1');
    });

    test('addComment calls API', () async {
      // Setup selected workspace
      when(mockRepository.getWorkspace('1')).thenAnswer((_) async =>
        Workspace(
          id: '1',
          name: 'WS',
          ownerId: 'user1',
          createdAt: DateTime.now(),
          memberCount: 1,
          savedListingCount: 0,
        )
      );
      when(mockRepository.getWorkspaceMembers('1')).thenAnswer((_) async => []);
      when(mockRepository.getWorkspaceProperties('1')).thenAnswer((_) async => []);
      when(mockRepository.getWorkspaceActivity('1')).thenAnswer((_) async => []);
      await provider.selectWorkspace('1');

      when(mockRepository.addComment(any, any, any, any)).thenAnswer((_) async {});

      await provider.addComment('sl1', 'content', null);

      verify(mockRepository.addComment('1', 'sl1', 'content', null)).called(1);
    });

    test('fetchComments returns list', () async {
      // Setup selected workspace
      when(mockRepository.getWorkspace('1')).thenAnswer((_) async =>
        Workspace(
          id: '1',
          name: 'WS',
          ownerId: 'user1',
          createdAt: DateTime.now(),
          memberCount: 1,
          savedListingCount: 0,
        )
      );
      when(mockRepository.getWorkspaceMembers('1')).thenAnswer((_) async => []);
      when(mockRepository.getWorkspaceProperties('1')).thenAnswer((_) async => []);
      when(mockRepository.getWorkspaceActivity('1')).thenAnswer((_) async => []);
      await provider.selectWorkspace('1');

      when(mockRepository.fetchComments('1', 'sl1')).thenAnswer((_) async => [
        Comment(
          id: 'c1',
          userId: 'u1',
          content: 'Hello',
          createdAt: DateTime.now(),
          replies: [],
          reactions: {},
        )
      ]);

      final comments = await provider.fetchComments('sl1');
      expect(comments.length, 1);
      expect(comments.first.content, 'Hello');
    });

    test('deleteWorkspace removes workspace from list on success', () async {
      // Initial state
      final ws1 = Workspace(id: '1', name: 'WS1', ownerId: 'u1', createdAt: DateTime.now(), memberCount: 1, savedListingCount: 0);
      final ws2 = Workspace(id: '2', name: 'WS2', ownerId: 'u1', createdAt: DateTime.now(), memberCount: 1, savedListingCount: 0);

      when(mockRepository.fetchWorkspaces()).thenAnswer((_) async => [ws1, ws2]);
      await provider.fetchWorkspaces();
      expect(provider.workspaces.length, 2);

      // Delete logic
      when(mockRepository.deleteWorkspace('1')).thenAnswer((_) async => {});

      await provider.deleteWorkspace('1');

      expect(provider.workspaces.length, 1);
      expect(provider.workspaces.first.id, '2');
      expect(provider.isDeletingWorkspace, false);
      expect(provider.error, null);
    });

    test('deleteWorkspace sets error on failure', () async {
       // Initial state
      final ws1 = Workspace(id: '1', name: 'WS1', ownerId: 'u1', createdAt: DateTime.now(), memberCount: 1, savedListingCount: 0);
      when(mockRepository.fetchWorkspaces()).thenAnswer((_) async => [ws1]);
      await provider.fetchWorkspaces();

      // Delete failure
      when(mockRepository.deleteWorkspace('1')).thenThrow(Exception('Delete failed'));

      try {
        await provider.deleteWorkspace('1');
      } catch (_) {
        // Expected
      }

      expect(provider.workspaces.length, 1); // Not removed
      expect(provider.isDeletingWorkspace, false);
      expect(provider.error, contains('Delete failed'));
    });
  });
}
