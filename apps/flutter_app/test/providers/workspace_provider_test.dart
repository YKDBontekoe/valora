import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/mockito.dart';
import 'package:mockito/annotations.dart';
import 'package:valora_app/providers/workspace_provider.dart';
import 'package:valora_app/repositories/workspace_repository.dart';
import 'package:valora_app/models/workspace.dart';
import 'package:valora_app/models/saved_listing.dart';
import 'package:valora_app/models/comment.dart';
import 'package:valora_app/models/activity_log.dart';

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
    test('fetchWorkspaces populates list on success', () async {
      when(mockRepository.fetchWorkspaces()).thenAnswer((_) async => [
        Workspace(
          id: '1',
          name: 'Test',
          ownerId: 'user1',
          createdAt: DateTime.now(),
          memberCount: 1,
          savedListingCount: 0,
        )
      ]);

      await provider.fetchWorkspaces();

      expect(provider.workspaces.length, 1);
      expect(provider.workspaces.first.name, 'Test');
      expect(provider.isWorkspacesLoading, false);
      expect(provider.error, null);
    });

    test('fetchWorkspaces handles error', () async {
      when(mockRepository.fetchWorkspaces()).thenThrow(Exception('Network error'));

      await provider.fetchWorkspaces();

      expect(provider.workspaces.isEmpty, true);
      expect(provider.error, contains('Network error'));
      expect(provider.isWorkspacesLoading, false);
    });

    test('createWorkspace adds new workspace and updates list reference', () async {
      // Arrange
      final initialList = [
        Workspace(id: '1', name: 'Old', ownerId: 'u1', createdAt: DateTime.now(), memberCount: 0, savedListingCount: 0)
      ];
      // Inject initial state
      when(mockRepository.fetchWorkspaces()).thenAnswer((_) async => initialList);
      await provider.fetchWorkspaces();
      final listReferenceBefore = provider.workspaces;

      final newWs = Workspace(
        id: '2',
        name: 'New',
        ownerId: 'u1',
        createdAt: DateTime.now(),
        memberCount: 1,
        savedListingCount: 0,
      );

      when(mockRepository.createWorkspace(any, any)).thenAnswer((_) async => newWs);

      // Act
      await provider.createWorkspace('New', 'Desc');

      // Assert
      expect(provider.workspaces.length, 2);
      expect(provider.workspaces.first.id, '2'); // Newest first
      expect(provider.workspaces, isNot(same(listReferenceBefore))); // Verify immutability
    });

    test('selectWorkspace fetches details success', () async {
      when(mockRepository.getWorkspace('1')).thenAnswer((_) async =>
        Workspace(id: '1', name: 'WS', ownerId: 'user1', createdAt: DateTime.now(), memberCount: 1, savedListingCount: 0)
      );
      when(mockRepository.getWorkspaceMembers('1')).thenAnswer((_) async => []);
      when(mockRepository.getWorkspaceListings('1')).thenAnswer((_) async => []);
      when(mockRepository.getWorkspaceActivity('1')).thenAnswer((_) async => []);

      await provider.selectWorkspace('1');

      expect(provider.selectedWorkspace?.id, '1');
      expect(provider.isWorkspaceDetailLoading, false);
      expect(provider.error, null);
    });

    test('selectWorkspace handles error and resets loading', () async {
      // Arrange
      when(mockRepository.getWorkspace('1')).thenAnswer((_) async =>
        Workspace(id: '1', name: 'WS', ownerId: 'user1', createdAt: DateTime.now(), memberCount: 1, savedListingCount: 0)
      );
      // Simulate failure in one of the parallel calls
      when(mockRepository.getWorkspaceMembers('1')).thenThrow(Exception('Fetch members failed'));
      when(mockRepository.getWorkspaceListings('1')).thenAnswer((_) async => []);
      when(mockRepository.getWorkspaceActivity('1')).thenAnswer((_) async => []);

      // Act
      await provider.selectWorkspace('1');

      // Assert
      expect(provider.error, contains('Fetch members failed'));
      expect(provider.isWorkspaceDetailLoading, false);
      // Ensure partial state wasn't applied (selectedWorkspace should remain null if it started null, or previous value)
      expect(provider.selectedWorkspace, null);
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
      when(mockRepository.getWorkspaceListings('1')).thenAnswer((_) async => []);
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

    test('saveListing calls API and refreshes listings', () async {
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
      when(mockRepository.getWorkspaceListings('1')).thenAnswer((_) async => []);
      when(mockRepository.getWorkspaceActivity('1')).thenAnswer((_) async => []);
      await provider.selectWorkspace('1');

      // Setup save call
      when(mockRepository.saveListing(any, any, any)).thenAnswer((_) async {});
      when(mockRepository.getWorkspaceListings('1')).thenAnswer((_) async => [
        SavedListing(
          id: 'sl1',
          listingId: 'l1',
          addedByUserId: 'u1',
          addedAt: DateTime.now(),
          commentCount: 0,
          listing: ListingSummary(id: 'l1', address: '123 St')
        )
      ]);

      await provider.saveListing('l1', 'notes');

      expect(provider.savedListings.length, 1);
      expect(provider.savedListings.first.listingId, 'l1');
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
      when(mockRepository.getWorkspaceListings('1')).thenAnswer((_) async => []);
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
      when(mockRepository.getWorkspaceListings('1')).thenAnswer((_) async => []);
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
  });
}
