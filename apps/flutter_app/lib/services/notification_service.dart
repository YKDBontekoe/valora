import 'dart:async';
import 'package:flutter/foundation.dart';
import '../models/notification.dart';
import 'api_service.dart';

class NotificationService extends ChangeNotifier {
  ApiService _apiService;
  List<ValoraNotification> _notifications = [];
  int _unreadCount = 0;
  bool _isLoading = false;
  String? _error;
  Timer? _pollingTimer;

  NotificationService(this._apiService);


  void update(ApiService apiService) {
    _apiService = apiService;
  }

  List<ValoraNotification> get notifications => _notifications;
  int get unreadCount => _unreadCount;
  bool get isLoading => _isLoading;
  String? get error => _error;

  void startPolling() {
    _pollingTimer?.cancel();
    _fetchUnreadCount(); // Initial fetch
    _pollingTimer = Timer.periodic(const Duration(seconds: 30), (_) {
      _fetchUnreadCount();
    });
  }

  void stopPolling() {
    _pollingTimer?.cancel();
    _pollingTimer = null;
  }

  Future<void> _fetchUnreadCount() async {
    try {
      final count = await _apiService.getUnreadNotificationCount();
      if (_unreadCount != count) {
        _unreadCount = count;
        notifyListeners();
      }
    } catch (e) {
      // Silently fail on polling errors
      if (kDebugMode) {
        print('Error polling notifications: $e');
      }
    }
  }

  Future<void> deleteNotification(String id) async {
    final index = _notifications.indexWhere((n) => n.id == id);
    if (index == -1) return;

    final removed = _notifications[index];

    // Optimistic update
    _notifications.removeAt(index);
    if (!removed.isRead) {
      _unreadCount = _unreadCount > 0 ? _unreadCount - 1 : 0;
    }
    notifyListeners();

    try {
      await _apiService.deleteNotification(id);
    } catch (e) {
      if (kDebugMode) {
        print('Error deleting notification: $e');
      }
      // Revert state on failure
      if (index <= _notifications.length) {
        _notifications.insert(index, removed);
      } else {
        _notifications.add(removed);
      }

      if (!removed.isRead) {
        _unreadCount++;
      }
      notifyListeners();

      rethrow;
    }
  }

  Future<void> fetchNotifications({bool refresh = false}) async {
    if (_isLoading) return;

    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final fetched = await _apiService.getNotifications(limit: 50);
      _notifications = fetched;
      // Trust the specific endpoint for the badge, but for the list, we rely on what we fetched.
      // Sync unread count if we have the full list
      // _unreadCount = _notifications.where((n) => !n.isRead).length;
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> markAsRead(String id) async {
    try {
      // Optimistic update
      final index = _notifications.indexWhere((n) => n.id == id);
      if (index != -1 && !_notifications[index].isRead) {
        final old = _notifications[index];
        _notifications[index] = ValoraNotification(
          id: old.id,
          title: old.title,
          body: old.body,
          isRead: true,
          createdAt: old.createdAt,
          type: old.type,
          actionUrl: old.actionUrl,
        );
        _unreadCount = _unreadCount > 0 ? _unreadCount - 1 : 0;
        notifyListeners();

        await _apiService.markNotificationAsRead(id);
      }
    } catch (e) {
      if (kDebugMode) {
        print('Error marking notification as read: $e');
      }
      // Revert if needed, but for read status it's usually fine
    }
  }

  Future<void> markAllAsRead() async {
    try {
      // Optimistic update
      _notifications = _notifications.map((n) => ValoraNotification(
        id: n.id,
        title: n.title,
        body: n.body,
        isRead: true,
        createdAt: n.createdAt,
        type: n.type,
        actionUrl: n.actionUrl,
      )).toList();
      _unreadCount = 0;
      notifyListeners();

      await _apiService.markAllNotificationsAsRead();
    } catch (e) {
      if (kDebugMode) {
        print('Error marking all notifications as read: $e');
      }
    }
  }

  @override
  void dispose() {
    _pollingTimer?.cancel();
    super.dispose();
  }
}
