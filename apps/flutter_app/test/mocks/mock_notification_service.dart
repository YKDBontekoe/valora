import 'package:flutter/foundation.dart';
import 'package:mockito/mockito.dart';
import 'package:valora_app/services/notification_service.dart';
import 'package:valora_app/models/notification.dart';

class MockNotificationService extends Mock implements NotificationService {
  @override
  List<ValoraNotification> get notifications => [];

  @override
  int get unreadCount => 0;

  @override
  bool get isLoading => false;

  @override
  String? get error => null;

  @override
  bool get hasListeners => false;

  @override
  void addListener(VoidCallback? listener) {
    super.noSuchMethod(Invocation.method(#addListener, [listener]));
  }

  @override
  void removeListener(VoidCallback? listener) {
    super.noSuchMethod(Invocation.method(#removeListener, [listener]));
  }

  @override
  void dispose() {
    super.noSuchMethod(Invocation.method(#dispose, []));
  }

  @override
  void notifyListeners() {
    super.noSuchMethod(Invocation.method(#notifyListeners, []));
  }

  @override
  Future<void> fetchNotifications({bool refresh = false}) async {
    return super.noSuchMethod(Invocation.method(#fetchNotifications, [], {#refresh: refresh}), returnValue: Future.value(null));
  }

  @override
  void startPolling() {
    super.noSuchMethod(Invocation.method(#startPolling, []));
  }

  @override
  void stopPolling() {
    super.noSuchMethod(Invocation.method(#stopPolling, []));
  }
}
