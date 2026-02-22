import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:valora_app/models/workspace.dart';
import 'package:valora_app/models/saved_listing.dart';
import 'package:valora_app/models/comment.dart';
import 'package:valora_app/models/activity_log.dart';
import 'package:valora_app/providers/workspace_provider.dart';
import 'package:valora_app/services/api_client.dart';

import 'workspace_provider_test.mocks.dart';

@GenerateMocks([ApiClient])
void main() {
  late WorkspaceProvider provider;
  late MockApiClient mockApiClient;

  setUp(() {
    mockApiClient = MockApiClient();
    provider = WorkspaceProvider(mockApiClient);
  });

  group('WorkspaceProvider', () {
    test('fetchWorkspaces populates list on success', () async {
      when(mockApiClient.get('/api/workspaces')).thenAnswer((_) async => [
            {
              'id': '1',
              'name': 'Workspace 1',
              'description': 'Desc 1',
              'ownerId': 'user1',
              'createdAt': '2023-01-01T00:00:00Z',
              'updatedAt': '2023-01-01T00:00:00Z',
              'memberCount': 1,
              'savedListingCount': 0
            }
          ]);

      await provider.fetchWorkspaces();

      expect(provider.workspaces.length, 1);
      expect(provider.workspaces.first.name, 'Workspace 1');
      expect(provider.isLoading, false);
      expect(provider.error, null);
    });

    test('fetchWorkspaces handles error', () async {
      when(mockApiClient.get('/api/workspaces')).thenThrow(Exception('Network Error'));

      await provider.fetchWorkspaces();

      expect(provider.workspaces, isEmpty);
      expect(provider.isLoading, false);
      expect(provider.error, contains('Network Error'));
    });

    test('createWorkspace adds new workspace to list', () async {
      // Pre-populate
      // (Testing insert at 0 logic requires pre-existing list or check if list has 1 item)

      when(mockApiClient.post('/api/workspaces', any)).thenAnswer((_) async => {
            'id': '2',
            'name': 'New Workspace',
            'description': 'New Desc',
            'ownerId': 'user1',
            'createdAt': '2023-01-02T00:00:00Z',
            'updatedAt': '2023-01-02T00:00:00Z',
            'memberCount': 1,
            'savedListingCount': 0
          });

      await provider.createWorkspace('New Workspace', 'New Desc');

      expect(provider.workspaces.length, 1);
      expect(provider.workspaces.first.name, 'New Workspace');
    });

    test('selectWorkspace fetches details', () async {
      when(mockApiClient.get('/api/workspaces/1')).thenAnswer((_) async => {
            'id': '1',
            'name': 'W1',
            'ownerId': 'u1',
            'createdAt': '2023-01-01T00:00:00Z',
            'updatedAt': '2023-01-01T00:00:00Z',
            'memberCount': 1,
            'savedListingCount': 0
          });
      when(mockApiClient.get('/api/workspaces/1/members')).thenAnswer((_) async => []);
      when(mockApiClient.get('/api/workspaces/1/listings')).thenAnswer((_) async => []);
      when(mockApiClient.get('/api/workspaces/1/activity')).thenAnswer((_) async => []);

      await provider.selectWorkspace('1');

      expect(provider.selectedWorkspace, isNotNull);
      expect(provider.selectedWorkspace!.id, '1');
      expect(provider.isLoading, false);
    });
  });
}
