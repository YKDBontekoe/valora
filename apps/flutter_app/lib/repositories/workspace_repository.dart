import 'dart:convert';
import '../models/workspace.dart';
import '../models/saved_listing.dart';
import '../models/comment.dart';
import '../models/activity_log.dart';
import '../services/api_client.dart';

class WorkspaceRepository {
  final ApiClient _client;

  WorkspaceRepository(this._client);

  Future<List<Workspace>> fetchWorkspaces() async {
    final response = await _client.get('/api/workspaces');
    return _client.handleResponse(
      response,
      (body) {
        final List<dynamic> jsonList = json.decode(body);
        return jsonList.map((e) => Workspace.fromJson(e)).toList();
      },
    );
  }

  Future<Workspace> createWorkspace(String name, String? description) async {
    final response = await _client.post(
      '/api/workspaces',
      data: {
        'name': name,
        'description': description,
      },
    );
    return _client.handleResponse(
      response,
      (body) => Workspace.fromJson(json.decode(body)),
    );
  }

  Future<Workspace> updateWorkspace(String id, String name, String? description) async {
    final response = await _client.put(
      '/api/workspaces/$id',
      data: {
        'name': name,
        'description': description,
      },
    );
    return _client.handleResponse(
      response,
      (body) => Workspace.fromJson(json.decode(body)),
    );
  }

  Future<Workspace> getWorkspace(String id) async {
    final response = await _client.get('/api/workspaces/$id');
    return _client.handleResponse(
      response,
      (body) => Workspace.fromJson(json.decode(body)),
    );
  }

  Future<void> deleteWorkspace(String id) async {
    final response = await _client.delete('/api/workspaces/$id');
    await _client.handleResponse(response, (_) => null);
  }

  Future<List<WorkspaceMember>> getWorkspaceMembers(String id) async {
    final response = await _client.get('/api/workspaces/$id/members');
    return _client.handleResponse(
      response,
      (body) {
        final List<dynamic> jsonList = json.decode(body);
        return jsonList.map((e) => WorkspaceMember.fromJson(e)).toList();
      },
    );
  }

  Future<List<SavedListing>> getWorkspaceListings(String id) async {
    final response = await _client.get('/api/workspaces/$id/listings');
    return _client.handleResponse(
      response,
      (body) {
        final List<dynamic> jsonList = json.decode(body);
        return jsonList.map((e) => SavedListing.fromJson(e)).toList();
      },
    );
  }

  Future<List<ActivityLog>> getWorkspaceActivity(String id) async {
    final response = await _client.get('/api/workspaces/$id/activity');
    return _client.handleResponse(
      response,
      (body) {
        final List<dynamic> jsonList = json.decode(body);
        return jsonList.map((e) => ActivityLog.fromJson(e)).toList();
      },
    );
  }

  Future<void> inviteMember(String workspaceId, String email, String role) async {
    final response = await _client.post(
      '/api/workspaces/$workspaceId/members',
      data: {
        'email': email,
        'role': role,
      },
    );
    await _client.handleResponse(response, (_) => null);
  }

  Future<void> removeMember(String workspaceId, String memberId) async {
    final response = await _client.delete(
      '/api/workspaces/$workspaceId/members/$memberId',
    );
    await _client.handleResponse(response, (_) => null);
  }

  Future<void> saveListing(String workspaceId, String listingId, String? notes) async {
    final response = await _client.post(
      '/api/workspaces/$workspaceId/listings',
      data: {
        'listingId': listingId,
        'notes': notes,
      },
    );
    await _client.handleResponse(response, (_) => null);
  }

  Future<void> addComment(String workspaceId, String listingId, String content, String? parentId) async {
    final response = await _client.post(
      '/api/workspaces/$workspaceId/listings/$listingId/comments',
      data: {
        'content': content,
        'parentId': parentId,
      },
    );
    await _client.handleResponse(response, (_) => null);
  }

  Future<List<Comment>> fetchComments(String workspaceId, String listingId) async {
    final response = await _client.get(
      '/api/workspaces/$workspaceId/listings/$listingId/comments',
    );
    return _client.handleResponse(
      response,
      (body) {
        final List<dynamic> jsonList = json.decode(body);
        return jsonList.map((e) => Comment.fromJson(e)).toList();
      },
    );
  }
}
