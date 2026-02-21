import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/mockito.dart';
import 'package:mockito/annotations.dart';
import 'package:valora_app/providers/workspace_provider.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/models/workspace.dart';

@GenerateMocks([ApiService])
import 'workspace_provider_test.mocks.dart';

void main() {
  late WorkspaceProvider provider;
  late MockApiService mockApiService;

  setUp(() {
    mockApiService = MockApiService();
    provider = WorkspaceProvider(mockApiService);
  });

  group('WorkspaceProvider', () {
    test('fetchWorkspaces populates list on success', () async {
      when(mockApiService.get('/api/workspaces')).thenAnswer((_) async => [
        {
          'id': '1',
          'name': 'Test',
          'ownerId': 'user1',
          'createdAt': DateTime.now().toIso8601String(),
          'memberCount': 1,
          'savedListingCount': 0
        }
      ]);

      await provider.fetchWorkspaces();

      expect(provider.workspaces.length, 1);
      expect(provider.workspaces.first.name, 'Test');
      expect(provider.isLoading, false);
      expect(provider.error, null);
    });

    test('fetchWorkspaces handles error', () async {
      when(mockApiService.get('/api/workspaces')).thenThrow(Exception('Network error'));

      await provider.fetchWorkspaces();

      expect(provider.workspaces.isEmpty, true);
      expect(provider.error, contains('Network error'));
      expect(provider.isLoading, false);
    });

    test('createWorkspace adds new workspace', () async {
      when(mockApiService.post('/api/workspaces', any)).thenAnswer((_) async => {
        'id': '2',
        'name': 'New',
        'ownerId': 'user1',
        'createdAt': DateTime.now().toIso8601String(),
        'memberCount': 1,
        'savedListingCount': 0
      });

      await provider.createWorkspace('New', 'Desc');

      expect(provider.workspaces.length, 1);
      expect(provider.workspaces.first.id, '2');
    });

    test('selectWorkspace fetches details', () async {
      when(mockApiService.get('/api/workspaces/1')).thenAnswer((_) async => {
        'id': '1',
        'name': 'WS',
        'ownerId': 'user1',
        'createdAt': DateTime.now().toIso8601String(),
        'memberCount': 1,
        'savedListingCount': 0
      });
      when(mockApiService.get('/api/workspaces/1/members')).thenAnswer((_) async => []);
      when(mockApiService.get('/api/workspaces/1/listings')).thenAnswer((_) async => []);
      when(mockApiService.get('/api/workspaces/1/activity')).thenAnswer((_) async => []);

      await provider.selectWorkspace('1');

      expect(provider.selectedWorkspace?.id, '1');
      expect(provider.isLoading, false);
    });

    test('inviteMember calls API and refreshes members', () async {
      // Setup selected workspace
      when(mockApiService.get('/api/workspaces/1')).thenAnswer((_) async => {
        'id': '1',
        'name': 'WS',
        'ownerId': 'user1',
        'createdAt': DateTime.now().toIso8601String(),
        'memberCount': 1,
        'savedListingCount': 0
      });
      when(mockApiService.get('/api/workspaces/1/members')).thenAnswer((_) async => []);
      when(mockApiService.get('/api/workspaces/1/listings')).thenAnswer((_) async => []);
      when(mockApiService.get('/api/workspaces/1/activity')).thenAnswer((_) async => []);

      await provider.selectWorkspace('1');

      // Setup invite call
      when(mockApiService.post('/api/workspaces/1/members', any)).thenAnswer((_) async => {});
      when(mockApiService.get('/api/workspaces/1/members')).thenAnswer((_) async => [
        {
          'id': 'wm1',
          'userId': 'u2',
          'workspaceId': '1',
          'role': 'Viewer',
          'email': 'test@example.com',
          'isPending': false,
          'joinedAt': '2023-01-01T00:00:00Z'
        }
      ]);

      await provider.inviteMember('test@example.com', WorkspaceRole.viewer);

      expect(provider.members.length, 1);
      expect(provider.members.first.email, 'test@example.com');
    });

    test('saveListing calls API and refreshes listings', () async {
      // Setup selected workspace
      when(mockApiService.get('/api/workspaces/1')).thenAnswer((_) async => {
        'id': '1',
        'name': 'WS',
        'ownerId': 'user1',
        'createdAt': DateTime.now().toIso8601String(),
        'memberCount': 1,
        'savedListingCount': 0
      });
      when(mockApiService.get('/api/workspaces/1/members')).thenAnswer((_) async => []);
      when(mockApiService.get('/api/workspaces/1/listings')).thenAnswer((_) async => []);
      when(mockApiService.get('/api/workspaces/1/activity')).thenAnswer((_) async => []);
      await provider.selectWorkspace('1');

      // Setup save call
      when(mockApiService.post('/api/workspaces/1/listings', any)).thenAnswer((_) async => {});
      when(mockApiService.get('/api/workspaces/1/listings')).thenAnswer((_) async => [
        {
          'id': 'sl1',
          'listingId': 'l1',
          'addedByUserId': 'u1',
          'addedAt': '2023-01-01T00:00:00Z',
          'commentCount': 0,
          'listing': {
             'id': 'l1',
             'address': '123 St'
          }
        }
      ]);

      await provider.saveListing('l1', 'notes');

      expect(provider.savedListings.length, 1);
      expect(provider.savedListings.first.listingId, 'l1');
    });

    test('addComment calls API', () async {
      // Setup selected workspace
      when(mockApiService.get('/api/workspaces/1')).thenAnswer((_) async => {
        'id': '1',
        'name': 'WS',
        'ownerId': 'user1',
        'createdAt': DateTime.now().toIso8601String(),
        'memberCount': 1,
        'savedListingCount': 0
      });
      when(mockApiService.get('/api/workspaces/1/members')).thenAnswer((_) async => []);
      when(mockApiService.get('/api/workspaces/1/listings')).thenAnswer((_) async => []);
      when(mockApiService.get('/api/workspaces/1/activity')).thenAnswer((_) async => []);
      await provider.selectWorkspace('1');

      when(mockApiService.post('/api/workspaces/1/listings/sl1/comments', any)).thenAnswer((_) async => {});

      await provider.addComment('sl1', 'content', null);

      verify(mockApiService.post('/api/workspaces/1/listings/sl1/comments', {
        'content': 'content',
        'parentId': null,
      })).called(1);
    });

    test('fetchComments returns list', () async {
      // Setup selected workspace
      when(mockApiService.get('/api/workspaces/1')).thenAnswer((_) async => {
        'id': '1',
        'name': 'WS',
        'ownerId': 'user1',
        'createdAt': DateTime.now().toIso8601String(),
        'memberCount': 1,
        'savedListingCount': 0
      });
      when(mockApiService.get('/api/workspaces/1/members')).thenAnswer((_) async => []);
      when(mockApiService.get('/api/workspaces/1/listings')).thenAnswer((_) async => []);
      when(mockApiService.get('/api/workspaces/1/activity')).thenAnswer((_) async => []);
      await provider.selectWorkspace('1');

      when(mockApiService.get('/api/workspaces/1/listings/sl1/comments')).thenAnswer((_) async => [
        {'id': 'c1', 'savedListingId': 'sl1', 'userId': 'u1', 'content': 'Hello', 'createdAt': '2023-01-01T00:00:00Z', 'replies': []}
      ]);

      final comments = await provider.fetchComments('sl1');
      expect(comments.length, 1);
      expect(comments.first.content, 'Hello');
    });
  });
}
