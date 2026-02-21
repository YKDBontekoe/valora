import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:logging/logging.dart';
import 'package:signalr_netcore/signalr_client.dart';
import '../models/notification.dart';
import '../models/cursor_paged_result.dart';
import 'api_service.dart';

class NotificationService extends ChangeNotifier {
  static final _log = Logger('NotificationService');
  ApiService _apiService;
  List<ValoraNotification> _notifications = [];
  int _unreadCount = 0;
  bool _isLoading = false;
  bool _isLoadingMore = false;
  bool _hasMore = true;
  String? _nextCursor;
  static const int _pageSize = 20;
  String? _error;
  Timer? _pollingTimer;
  HubConnection? _hubConnection;

  final Map<String, Timer> _pendingDeletions = {};
  final Map<String, (int index, ValoraNotification notification)> _pendingDeletedNotifications = {};

  NotificationService(this._apiService);

  void update(ApiService apiService) {
    _apiService = apiService;
    if (_hubConnection == null && _apiService.authToken != null) {
      _initSignalR();
    }
  }

  List<ValoraNotification> get notifications => _notifications;
  int get unreadCount => _unreadCount;
  bool get isLoading => _isLoading;
  bool get isLoadingMore => _isLoadingMore;
  bool get hasMore => _hasMore;
  String? get error => _error;

  void startPolling() {
    _pollingTimer?.cancel();
    _fetchUnreadCount(); // Initial fetch
    _pollingTimer = Timer.periodic(const Duration(seconds: 30), (_) {
      _fetchUnreadCount();
      if (_hubConnection?.state == HubConnectionState.Disconnected) {
          _initSignalR();
      }
    });
    _initSignalR();
  }

  void stopPolling() {
    _pollingTimer?.cancel();
    _pollingTimer = null;
    _hubConnection?.stop();
    _hubConnection = null;
  }

  Future<void> _initSignalR() async {
    if (_hubConnection?.state == HubConnectionState.Connected) return;

    final token = _apiService.authToken;
    if (token == null) return;

    final uri = Uri.parse(ApiService.baseUrl);
    final hubUrl = uri.replace(path: '/hubs/notifications').toString();

    _hubConnection = HubConnectionBuilder()
        .withUrl(hubUrl, options: HttpConnectionOptions(
            accessTokenFactory: () async => token,
        ))
        .build();

    _hubConnection!.on('NotificationCreated', _handleNotificationCreated);
    _hubConnection!.on('NotificationRead', _handleNotificationRead);
    _hubConnection!.on('NotificationDeleted', _handleNotificationDeleted);

    try {
        await _hubConnection!.start();
        _log.info('SignalR Connected');
    } catch (e) {
        _log.warning('SignalR Connection Error: $e');
    }

    _hubConnection!.onclose(({error}) {
        _log.warning('SignalR Connection Closed: $error');
    });
  }

  @visibleForTesting
  void handleNotificationCreated(List<Object?>? args) => _handleNotificationCreated(args);

  void _handleNotificationCreated(List<Object?>? args) {
    if (args != null && args.isNotEmpty) {
        try {
            final map = args[0] as Map<String, dynamic>;
            final notification = ValoraNotification.fromJson(map);
            if (!_notifications.any((n) => n.id == notification.id)) {
                _notifications.insert(0, notification);
                _unreadCount++;
                notifyListeners();
            }
        } catch (e) {
            _log.warning('Error parsing notification event', e);
        }
    }
  }

  @visibleForTesting
  void handleNotificationRead(List<Object?>? args) => _handleNotificationRead(args);

  void _handleNotificationRead(List<Object?>? args) {
    if (args != null && args.isNotEmpty) {
        final id = args[0] as String;
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
        }
    }
  }

  @visibleForTesting
  void handleNotificationDeleted(List<Object?>? args) => _handleNotificationDeleted(args);

  void _handleNotificationDeleted(List<Object?>? args) {
     if (args != null && args.isNotEmpty) {
        final id = args[0] as String;
        final index = _notifications.indexWhere((n) => n.id == id);
        if (index != -1) {
            final removed = _notifications[index];
            _notifications.removeAt(index);
            if (!removed.isRead) {
                _unreadCount = _unreadCount > 0 ? _unreadCount - 1 : 0;
            }
            notifyListeners();
        }
    }
  }

  Future<void> _fetchUnreadCount() async {
    try {
      int count = await _apiService.getUnreadNotificationCount();

      for (final pending in _pendingDeletedNotifications.values) {
        if (!pending.$2.isRead) {
          count = count > 0 ? count - 1 : 0;
        }
      }

      if (_unreadCount != count) {
        _unreadCount = count;
        notifyListeners();
      }
    } catch (e) {
      _log.warning('Error polling notifications', e);
    }
  }

  Future<void> deleteNotification(String id) async {
    if (_pendingDeletions.containsKey(id)) {
        return;
    }

    final index = _notifications.indexWhere((n) => n.id == id);
    if (index == -1) return;

    final removed = _notifications[index];

    _pendingDeletedNotifications[id] = (index, removed);

    _notifications.removeAt(index);
    if (!removed.isRead) {
      _unreadCount = _unreadCount > 0 ? _unreadCount - 1 : 0;
    }
    notifyListeners();

    _pendingDeletions[id] = Timer(const Duration(seconds: 4), () async {
      _pendingDeletions.remove(id);
      final pendingRestore = _pendingDeletedNotifications.remove(id);

      try {
        await _apiService.deleteNotification(id);
      } catch (e) {
        _log.warning('Error deleting notification', e);

        if (pendingRestore != null) {
          final (idx, notification) = pendingRestore;
          if (idx >= 0 && idx <= _notifications.length) {
            _notifications.insert(idx, notification);
          } else {
            _notifications.add(notification);
          }
          if (!notification.isRead) {
            _unreadCount++;
          }

          _error = "Failed to delete notification";
          notifyListeners();
        }
      }
    });
  }

  Future<void> fetchNotifications({bool refresh = false}) async {
    if (_isLoading) return;

    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final results = await Future.wait([
        _apiService.getNotifications(limit: _pageSize, cursor: null),
        _apiService.getUnreadNotificationCount(),
      ]);

      var result = results[0] as CursorPagedResult<ValoraNotification>;
      var count = results[1] as int;

      var fetched = result.items;

      fetched = fetched.where((n) => !_pendingDeletions.containsKey(n.id)).toList();

      for (final pending in _pendingDeletedNotifications.values) {
        if (!pending.$2.isRead) {
          count = count > 0 ? count - 1 : 0;
        }
      }

      _notifications = fetched;
      _unreadCount = count;
      _hasMore = result.hasMore;
      _nextCursor = result.nextCursor;
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> loadMoreNotifications() async {
    if (_isLoading || _isLoadingMore || !_hasMore) return;

    _isLoadingMore = true;
    if (_error != null) {
      _error = null;
    }
    notifyListeners();

    try {
      final result = await _apiService.getNotifications(limit: _pageSize, cursor: _nextCursor);

      if (result.items.isEmpty) {
        _hasMore = false;
      } else {
        _notifications.addAll(result.items);
        _hasMore = result.hasMore;
        _nextCursor = result.nextCursor;
      }
    } catch (e) {
      _log.warning('Error loading more notifications', e);
    } finally {
      _isLoadingMore = false;
      notifyListeners();
    }
  }

  Future<void> markAsRead(String id) async {
    try {
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
      _log.warning('Error marking notification as read', e);
    }
  }

  Future<void> markAllAsRead() async {
    try {
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
      _log.warning('Error marking all notifications as read', e);
    }
  }

  void undoDelete(String id) {
    if (!_pendingDeletions.containsKey(id)) return;

    _pendingDeletions[id]?.cancel();
    _pendingDeletions.remove(id);

    if (_pendingDeletedNotifications.containsKey(id)) {
      final (index, notification) = _pendingDeletedNotifications[id]!;
      _pendingDeletedNotifications.remove(id);

      if (index >= 0 && index <= _notifications.length) {
        _notifications.insert(index, notification);
      } else {
        _notifications.add(notification);
      }

      if (!notification.isRead) {
        _unreadCount++;
      }
      notifyListeners();
    }
  }

  @override
  void dispose() {
    _pollingTimer?.cancel();
    _hubConnection?.stop();
    for (final timer in _pendingDeletions.values) {
      timer.cancel();
    }
    _pendingDeletions.clear();
    _pendingDeletedNotifications.clear();
    super.dispose();
  }
}
