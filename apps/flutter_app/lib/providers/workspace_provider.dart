import 'package:flutter/material.dart';
import '../models/workspace.dart';
import '../models/saved_property.dart';
import '../models/comment.dart';
import '../models/activity_log.dart';
import '../repositories/workspace_repository.dart';

class WorkspaceProvider extends ChangeNotifier {
  final WorkspaceRepository _repository;

  List<Workspace> _workspaces = [];
  List<Workspace> get workspaces => _workspaces;

  bool _isWorkspacesLoading = false;
  bool get isWorkspacesLoading => _isWorkspacesLoading;

  bool _isWorkspaceDetailLoading = false;
  bool get isWorkspaceDetailLoading => _isWorkspaceDetailLoading;

  bool _isDeletingWorkspace = false;
  bool get isDeletingWorkspace => _isDeletingWorkspace;

  String? _error;
  String? get error => _error;

  Workspace? _selectedWorkspace;
  Workspace? get selectedWorkspace => _selectedWorkspace;
  List<WorkspaceMember> _members = [];
  List<WorkspaceMember> get members => _members;
  List<SavedProperty> _savedProperties = [];
  List<SavedProperty> get savedProperties => _savedProperties;
  List<ActivityLog> _activityLogs = [];
  List<ActivityLog> get activityLogs => _activityLogs;

  WorkspaceProvider(this._repository);

  Future<void> fetchWorkspaces() async {
    _isWorkspacesLoading = true;
    _error = null;
    notifyListeners();

    try {
      _workspaces = await _repository.fetchWorkspaces();
    } catch (e) {
      _error = e.toString();
    } finally {
      _isWorkspacesLoading = false;
      notifyListeners();
    }
  }

  Future<void> createWorkspace(String name, String? description) async {
    try {
      final newWorkspace = await _repository.createWorkspace(name, description);
      _workspaces = [newWorkspace, ..._workspaces];
      notifyListeners();
    } catch (e) {
      rethrow;
    }
  }

  Future<void> deleteWorkspace(String id) async {
    _isDeletingWorkspace = true;
    _error = null;
    notifyListeners();

    try {
      await _repository.deleteWorkspace(id);
      _workspaces = _workspaces.where((w) => w.id != id).toList();
    } catch (e) {
      _error = e.toString();
      rethrow;
    } finally {
      _isDeletingWorkspace = false;
      notifyListeners();
    }
  }

  Future<void> selectWorkspace(String id) async {
    _isWorkspaceDetailLoading = true;
    _error = null;
    notifyListeners();
    try {
      final workspaceFuture = _repository.getWorkspace(id);
      final membersFuture = _repository.getWorkspaceMembers(id);
      final propertiesFuture = _repository.getWorkspaceProperties(id);
      final activityFuture = _repository.getWorkspaceActivity(id);

      final results = await Future.wait([
        workspaceFuture,
        membersFuture,
        propertiesFuture,
        activityFuture,
      ]);

      _selectedWorkspace = results[0] as Workspace;
      _members = results[1] as List<WorkspaceMember>;
      _savedProperties = results[2] as List<SavedProperty>;
      _activityLogs = results[3] as List<ActivityLog>;

    } catch (e) {
      _error = e.toString();
    } finally {
      _isWorkspaceDetailLoading = false;
      notifyListeners();
    }
  }

  Future<void> inviteMember(String email, WorkspaceRole role) async {
    if (_selectedWorkspace == null) return;
    try {
      await _repository.inviteMember(_selectedWorkspace!.id, email, role.name);
      _members = await _repository.getWorkspaceMembers(_selectedWorkspace!.id);
      notifyListeners();
    } catch (e) {
      rethrow;
    }
  }

  Future<void> saveProperty(String propertyId, String? notes) async {
    if (_selectedWorkspace == null) return;
    try {
      await _repository.saveProperty(_selectedWorkspace!.id, propertyId, notes);
      _savedProperties = await _repository.getWorkspaceProperties(_selectedWorkspace!.id);
      notifyListeners();
    } catch (e) {
      rethrow;
    }
  }

  Future<void> addComment(String savedPropertyId, String content, String? parentId) async {
     if (_selectedWorkspace == null) return;
     try {
       await _repository.addComment(_selectedWorkspace!.id, savedPropertyId, content, parentId);
       notifyListeners();
     } catch (e) {
       rethrow;
     }
  }

  Future<List<Comment>> fetchComments(String savedPropertyId) async {
    if (_selectedWorkspace == null) return [];
    try {
      return await _repository.fetchComments(_selectedWorkspace!.id, savedPropertyId);
    } catch (e) {
      rethrow;
    }
  }
}
