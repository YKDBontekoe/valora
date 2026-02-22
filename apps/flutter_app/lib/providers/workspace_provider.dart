import 'package:flutter/material.dart';
import '../models/workspace.dart';
import '../models/saved_listing.dart';
import '../models/comment.dart';
import '../models/activity_log.dart';
import '../repositories/workspace_repository.dart';

class WorkspaceProvider extends ChangeNotifier {
  final WorkspaceRepository _repository;

  List<Workspace> _workspaces = [];
  List<Workspace> get workspaces => _workspaces;

  bool _isLoading = false;
  bool get isLoading => _isLoading;

  String? _error;
  String? get error => _error;

  Workspace? _selectedWorkspace;
  Workspace? get selectedWorkspace => _selectedWorkspace;
  List<WorkspaceMember> _members = [];
  List<WorkspaceMember> get members => _members;
  List<SavedListing> _savedListings = [];
  List<SavedListing> get savedListings => _savedListings;
  List<ActivityLog> _activityLogs = [];
  List<ActivityLog> get activityLogs => _activityLogs;

  WorkspaceProvider(this._repository);

  Future<void> fetchWorkspaces() async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      _workspaces = await _repository.fetchWorkspaces();
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> createWorkspace(String name, String? description) async {
    try {
      final newWorkspace = await _repository.createWorkspace(name, description);
      _workspaces.insert(0, newWorkspace);
      notifyListeners();
    } catch (e) {
      rethrow;
    }
  }

  Future<void> selectWorkspace(String id) async {
    _isLoading = true;
    _error = null;
    notifyListeners();
    try {
      _selectedWorkspace = await _repository.getWorkspace(id);

      final results = await Future.wait([
        _repository.getWorkspaceMembers(id),
        _repository.getWorkspaceListings(id),
        _repository.getWorkspaceActivity(id),
      ]);

      _members = results[0] as List<WorkspaceMember>;
      _savedListings = results[1] as List<SavedListing>;
      _activityLogs = results[2] as List<ActivityLog>;

    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
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

  Future<void> saveListing(String listingId, String? notes) async {
    if (_selectedWorkspace == null) return;
    try {
      await _repository.saveListing(_selectedWorkspace!.id, listingId, notes);
      _savedListings = await _repository.getWorkspaceListings(_selectedWorkspace!.id);
      notifyListeners();
    } catch (e) {
      rethrow;
    }
  }

  Future<void> addComment(String savedListingId, String content, String? parentId) async {
     if (_selectedWorkspace == null) return;
     try {
       await _repository.addComment(_selectedWorkspace!.id, savedListingId, content, parentId);
       notifyListeners();
     } catch (e) {
       rethrow;
     }
  }

  Future<List<Comment>> fetchComments(String savedListingId) async {
    if (_selectedWorkspace == null) return [];
    try {
      return await _repository.fetchComments(_selectedWorkspace!.id, savedListingId);
    } catch (e) {
      rethrow;
    }
  }
}
