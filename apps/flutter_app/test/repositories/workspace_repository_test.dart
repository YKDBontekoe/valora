import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:valora_app/repositories/workspace_repository.dart';
import 'package:valora_app/services/api_client.dart';

@GenerateMocks([ApiClient])
import 'workspace_repository_test.mocks.dart';

void main() {
  late WorkspaceRepository repository;
  late MockApiClient mockClient;

  setUp(() {
    mockClient = MockApiClient();
    repository = WorkspaceRepository(mockClient);
  });

  group('WorkspaceRepository', () {
    test('fetchWorkspaces parses list correctly', () async {
      final jsonBody = jsonEncode([
        {'id': '1', 'name': 'WS', 'ownerId': 'u1', 'createdAt': DateTime.now().toIso8601String(), 'memberCount': 1, 'savedListingCount': 0}
      ]);
      final response = http.Response(jsonBody, 200);

      when(mockClient.get('/api/workspaces'))
          .thenAnswer((_) async => response);

      when(mockClient.handleResponse(any, any)).thenAnswer((invocation) {
        final parser = invocation.positionalArguments[1] as Function;
        return parser(jsonBody);
      });

      final result = await repository.fetchWorkspaces();

      expect(result.length, 1);
      expect(result.first.name, 'WS');
    });

    test('createWorkspace returns workspace object', () async {
      final jsonBody = jsonEncode(
        {'id': '1', 'name': 'New', 'ownerId': 'u1', 'createdAt': DateTime.now().toIso8601String(), 'memberCount': 1, 'savedListingCount': 0}
      );
      final response = http.Response(jsonBody, 200);

      when(mockClient.post('/api/workspaces', data: anyNamed('data')))
          .thenAnswer((_) async => response);

      when(mockClient.handleResponse(any, any)).thenAnswer((invocation) {
        final parser = invocation.positionalArguments[1] as Function;
        return parser(jsonBody);
      });

      final result = await repository.createWorkspace('New', 'Desc');

      expect(result.name, 'New');
    });

    test('getWorkspace returns workspace object', () async {
      final jsonBody = jsonEncode(
        {'id': '1', 'name': 'WS', 'ownerId': 'u1', 'createdAt': DateTime.now().toIso8601String(), 'memberCount': 1, 'savedListingCount': 0}
      );
      final response = http.Response(jsonBody, 200);

      when(mockClient.get('/api/workspaces/1'))
          .thenAnswer((_) async => response);

      when(mockClient.handleResponse(any, any)).thenAnswer((invocation) {
        final parser = invocation.positionalArguments[1] as Function;
        return parser(jsonBody);
      });

      final result = await repository.getWorkspace('1');

      expect(result.id, '1');
    });

    test('inviteMember calls correct endpoint', () async {
      final response = http.Response('', 204);

      when(mockClient.post('/api/workspaces/1/members', data: anyNamed('data')))
          .thenAnswer((_) async => response);

      when(mockClient.handleResponse(any, any)).thenAnswer((_) async => null);

      await repository.inviteMember('1', 'test@example.com', 'Viewer');

      verify(mockClient.post(
        '/api/workspaces/1/members',
        data: {'email': 'test@example.com', 'role': 'Viewer'},
      )).called(1);
    });
  });
}
