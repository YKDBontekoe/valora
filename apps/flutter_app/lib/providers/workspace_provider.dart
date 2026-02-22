import 'package:flutter/material.dart';
import '../models/workspace.dart';
import '../models/saved_listing.dart';
import '../models/comment.dart';
import '../models/activity_log.dart';
import '../services/api_service.dart';

class WorkspaceProvider extends ChangeNotifier {
  final ApiService _apiService;

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

  WorkspaceProvider(this._apiService);

  Future<void> fetchWorkspaces() async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final List<dynamic> data = await _apiService.get('/api/workspaces');
      _workspaces = data.map((json) => Workspace.fromJson(json)).toList();
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> createWorkspace(String name, String? description) async {
    try {
      final data = await _apiService.post('/api/workspaces', {
        'name': name,
        'description': description,
      });
      final newWorkspace = Workspace.fromJson(data);
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
      final wData = await _apiService.get('/api/workspaces/$id');
      _selectedWorkspace = Workspace.fromJson(wData);

      final mData = await _apiService.get('/api/workspaces/$id/members');
      _members = (mData as List)
          .map((j) => WorkspaceMember.fromJson(j))
          .toList();

      final sData = await _apiService.get('/api/workspaces/$id/listings');
      _savedListings = (sData as List)
          .map((j) => SavedListing.fromJson(j))
          .toList();

      final aData = await _apiService.get('/api/workspaces/$id/activity');
      _activityLogs = (aData as List)
          .map((j) => ActivityLog.fromJson(j))
          .toList();
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
      await _apiService.post(
        '/api/workspaces/${_selectedWorkspace!.id}/members',
        {'email': email, 'role': role.name},
      );
      final mData = await _apiService.get(
        '/api/workspaces/${_selectedWorkspace!.id}/members',
      );
      _members = (mData as List)
          .map((j) => WorkspaceMember.fromJson(j))
          .toList();
      notifyListeners();
    } catch (e) {
      rethrow;
    }
  }

  Future<void> saveListing(String listingId, String? notes) async {
    if (_selectedWorkspace == null) return;
    try {
      await _apiService.post(
        '/api/workspaces/${_selectedWorkspace!.id}/listings',
        {'listingId': listingId, 'notes': notes},
      );
      final sData = await _apiService.get(
        '/api/workspaces/${_selectedWorkspace!.id}/listings',
      );
      _savedListings = (sData as List)
          .map((j) => SavedListing.fromJson(j))
          .toList();
      notifyListeners();
    } catch (e) {
      rethrow;
    }
  }

  Future<void> addComment(
    String savedListingId,
    String content,
    String? parentId,
  ) async {
    if (_selectedWorkspace == null) return;
    try {
      await _apiService.post(
        '/api/workspaces/${_selectedWorkspace!.id}/listings/$savedListingId/comments',
        {'content': content, 'parentId': parentId},
      );
      notifyListeners();
    } catch (e) {
      rethrow;
    }
  }

  Future<List<Comment>> fetchComments(String savedListingId) async {
    if (_selectedWorkspace == null) return [];
    try {
      final List<dynamic> data = await _apiService.get(
        '/api/workspaces/${_selectedWorkspace!.id}/listings/$savedListingId/comments',
      );
      return data.map((j) => Comment.fromJson(j)).toList();
    } catch (e) {
      rethrow;
    }
  }
}
