import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:valora_app/models/workspace.dart';
import 'package:valora_app/providers/workspace_provider.dart';
import 'package:valora_app/repositories/workspace_repository.dart';

import 'workspace_provider_test.mocks.dart';

@GenerateMocks([WorkspaceRepository])
void main() {
  late MockWorkspaceRepository mockRepository;
  late WorkspaceProvider provider;

  setUp(() {
    mockRepository = MockWorkspaceRepository();
    provider = WorkspaceProvider(mockRepository);
  });

  group('WorkspaceProvider', () {
    final workspace1 = Workspace(
      id: '1',
      name: 'Workspace 1',
      description: 'Desc 1',
      ownerId: 'owner',
      createdAt: DateTime(2024, 1, 1),
      memberCount: 1,
      savedListingCount: 0,
    );

    final workspace2 = Workspace(
      id: '2',
      name: 'Workspace 2',
      description: 'Desc 2',
      ownerId: 'owner',
      createdAt: DateTime(2024, 1, 2),
      memberCount: 2,
      savedListingCount: 0,
    );

    test('updateWorkspace updates the list and selected workspace', () async {
      // Setup initial state
      when(mockRepository.fetchWorkspaces()).thenAnswer((_) async => [workspace1]);
      await provider.fetchWorkspaces();

      // Select it
      when(mockRepository.getWorkspace('1')).thenAnswer((_) async => workspace1);
      when(mockRepository.getWorkspaceMembers('1')).thenAnswer((_) async => []);
      when(mockRepository.getWorkspaceListings('1')).thenAnswer((_) async => []);
      when(mockRepository.getWorkspaceActivity('1')).thenAnswer((_) async => []);
      await provider.selectWorkspace('1');

      final updatedWorkspace = Workspace(
        id: '1',
        name: 'Updated Name',
        description: 'Updated Desc',
        ownerId: 'owner',
        createdAt: workspace1.createdAt,
        memberCount: 1,
        savedListingCount: 0,
      );

      when(mockRepository.updateWorkspace('1', 'Updated Name', 'Updated Desc'))
          .thenAnswer((_) async => updatedWorkspace);

      await provider.updateWorkspace('1', 'Updated Name', 'Updated Desc');

      // Verify list update
      expect(provider.workspaces.first.name, 'Updated Name');
      // Verify selected workspace update
      expect(provider.selectedWorkspace?.name, 'Updated Name');
    });

    test('deleteWorkspace removes from list and clears selection', () async {
      // Setup initial state with 2 workspaces
      when(mockRepository.fetchWorkspaces()).thenAnswer((_) async => [workspace1, workspace2]);
      await provider.fetchWorkspaces();

      // Select workspace 1
      when(mockRepository.getWorkspace('1')).thenAnswer((_) async => workspace1);
      when(mockRepository.getWorkspaceMembers('1')).thenAnswer((_) async => []);
      when(mockRepository.getWorkspaceListings('1')).thenAnswer((_) async => []);
      when(mockRepository.getWorkspaceActivity('1')).thenAnswer((_) async => []);
      await provider.selectWorkspace('1');

      expect(provider.selectedWorkspace, isNotNull);

      // Delete workspace 1
      when(mockRepository.deleteWorkspace('1')).thenAnswer((_) async {});

      await provider.deleteWorkspace('1');

      // Verify removed from list
      expect(provider.workspaces.length, 1);
      expect(provider.workspaces.first.id, '2');

      // Verify selection cleared
      expect(provider.selectedWorkspace, isNull);
      expect(provider.members, isEmpty);
      expect(provider.savedListings, isEmpty);
      expect(provider.activityLogs, isEmpty);
    });
  });
}
