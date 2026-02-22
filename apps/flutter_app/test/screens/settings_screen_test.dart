import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/providers/auth_provider.dart';
import 'package:valora_app/providers/settings_provider.dart';
import 'package:valora_app/providers/theme_provider.dart';
import 'package:valora_app/providers/workspace_provider.dart';
import 'package:valora_app/screens/settings_screen.dart';
import 'package:valora_app/models/workspace.dart';
import 'package:valora_app/models/saved_listing.dart';
import 'package:valora_app/models/comment.dart';
import 'package:valora_app/models/activity_log.dart';

// Mocks
class MockAuthProvider extends ChangeNotifier implements AuthProvider {
  @override
  String? get email => 'test@valora.nl';
  @override
  bool get isAuthenticated => true;
  @override
  bool get isLoading => false;
  @override
  String? get token => 'fake-token';

  bool logoutCalled = false;

  @override
  Future<void> logout() async {
    logoutCalled = true;
    notifyListeners();
  }

  @override
  Future<void> checkAuth() async {}
  @override
  Future<void> login(String email, String password) async {}
  @override
  Future<void> loginWithGoogle() async {}
  @override
  Future<void> register(String email, String password, String confirmPassword) async {}
  @override
  Future<String?> refreshSession() async => null;
}

class MockThemeProvider extends ChangeNotifier implements ThemeProvider {
  @override
  bool get isDarkMode => false;
  @override
  ThemeMode get themeMode => ThemeMode.light;
  @override
  bool get isInitialized => true;

  @override
  void toggleTheme() {
    notifyListeners();
  }

  @override
  void setThemeMode(ThemeMode mode) {}
}

class MockSettingsProvider extends ChangeNotifier implements SettingsProvider {
  @override
  double get reportRadius => 500.0;
  @override
  String get mapDefaultMetric => 'price';
  @override
  bool get notificationsEnabled => true;
  @override
  String get notificationFrequency => 'daily';
  @override
  bool get diagnosticsEnabled => false;
  @override
  String get appVersion => '1.0.0';
  @override
  String get buildNumber => '1';
  @override
  bool get isInitialized => true;

  bool setNotificationsCalled = false;
  bool setReportRadiusCalled = false;
  bool clearAllDataCalled = false;

  @override
  Future<void> setReportRadius(double value) async {
    setReportRadiusCalled = true;
    notifyListeners();
  }

  @override
  Future<void> setMapDefaultMetric(String value) async {}

  @override
  Future<void> setNotificationsEnabled(bool value) async {
    setNotificationsCalled = true;
    notifyListeners();
  }

  @override
  Future<void> setNotificationFrequency(String value) async {}

  @override
  Future<void> setDiagnosticsEnabled(bool value) async {}

  @override
  Future<void> clearAllData(BuildContext context) async {
    clearAllDataCalled = true;
    notifyListeners();
  }
}

class MockWorkspaceProvider extends ChangeNotifier implements WorkspaceProvider {
  @override
  List<Workspace> get workspaces => [];
  @override
  bool get isLoading => false;
  @override
  String? get error => null;
  @override
  Workspace? get selectedWorkspace => null;
  @override
  List<WorkspaceMember> get members => [];
  @override
  List<SavedListing> get savedListings => [];
  @override
  List<ActivityLog> get activityLogs => [];

  @override
  Future<void> fetchWorkspaces() async {}
  @override
  Future<void> createWorkspace(String name, String? description) async {}
  @override
  Future<void> selectWorkspace(String id) async {}
  @override
  Future<void> inviteMember(String email, WorkspaceRole role) async {}
  @override
  Future<void> saveListing(String listingId, String? notes) async {}
  @override
  Future<void> addComment(String savedListingId, String content, String? parentId) async {}
  @override
  Future<List<Comment>> fetchComments(String savedListingId) async => [];
  @override
  Future<void> deleteWorkspace(String id) async {}
  @override
  Future<void> updateWorkspace(String id, String name, String? description) async {}
  @override
  Future<void> removeMember(String workspaceId, String userId) async {}
  @override
  Future<void> updateMemberRole(String workspaceId, String userId, String role) async {}
  @override
  Future<void> deleteComment(String workspaceId, String savedListingId, String commentId) async {}
}

void main() {
  late MockAuthProvider mockAuthProvider;
  late MockThemeProvider mockThemeProvider;
  late MockSettingsProvider mockSettingsProvider;
  late MockWorkspaceProvider mockWorkspaceProvider;

  setUp(() {
    mockAuthProvider = MockAuthProvider();
    mockThemeProvider = MockThemeProvider();
    mockSettingsProvider = MockSettingsProvider();
    mockWorkspaceProvider = MockWorkspaceProvider();
  });

  Widget createWidgetUnderTest() {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider<AuthProvider>.value(value: mockAuthProvider),
        ChangeNotifierProvider<ThemeProvider>.value(value: mockThemeProvider),
        ChangeNotifierProvider<SettingsProvider>.value(value: mockSettingsProvider),
        ChangeNotifierProvider<WorkspaceProvider>.value(value: mockWorkspaceProvider),
      ],
      child: const MaterialApp(
        home: SettingsScreen(),
      ),
    );
  }

  testWidgets('SettingsScreen renders all sections correctly', (WidgetTester tester) async {
    tester.view.physicalSize = const Size(2000, 3000);
    tester.view.devicePixelRatio = 2.0;

    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle();

    expect(find.text('MAP & REPORTS'), findsOneWidget);
    expect(find.text('PREFERENCES'), findsOneWidget);
    expect(find.text('PRIVACY & DATA'), findsOneWidget);

    expect(find.text('Report Radius'), findsOneWidget);
    expect(find.text('500m'), findsOneWidget);
    expect(find.byType(Slider), findsOneWidget);

    expect(find.text('Notifications'), findsOneWidget);
    expect(find.text('Frequency'), findsOneWidget);

    expect(find.text('Data Management'), findsOneWidget);
    expect(find.text('Clear Cache & History'), findsOneWidget);

    expect(find.text('Diagnostics'), findsOneWidget);

    expect(find.text('Valora v1.0.0 (Build 1)'), findsOneWidget);

    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });
  });

  testWidgets('Toggling notifications calls provider', (WidgetTester tester) async {
    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle();

    final switchFinder = find.byType(Switch).first;
    await tester.tap(switchFinder);
    await tester.pump();

    expect(mockSettingsProvider.setNotificationsCalled, isTrue);
  });

  testWidgets('Clear Data shows confirmation dialog and calls provider', (WidgetTester tester) async {
    tester.view.physicalSize = const Size(2000, 3000);
    tester.view.devicePixelRatio = 2.0;

    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle();

    await tester.drag(find.byType(CustomScrollView), const Offset(0, -500));
    await tester.pumpAndSettle();

    final clearButtonFinder = find.text('Clear Cache & History');
    expect(clearButtonFinder, findsOneWidget);
    await tester.tap(clearButtonFinder);
    await tester.pumpAndSettle();

    expect(find.text('Clear All Data?'), findsOneWidget);

    await tester.tap(find.text('Clear Data'));
    await tester.pumpAndSettle();

    expect(mockSettingsProvider.clearAllDataCalled, isTrue);

    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });
  });

  testWidgets('Logout shows confirmation dialog and calls provider', (WidgetTester tester) async {
    tester.view.physicalSize = const Size(2000, 3000);
    tester.view.devicePixelRatio = 2.0;

    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle();

    await tester.drag(find.byType(CustomScrollView), const Offset(0, -1000));
    await tester.pumpAndSettle();

    final logoutButtonFinder = find.text('Log Out');
    expect(logoutButtonFinder, findsOneWidget);
    await tester.tap(logoutButtonFinder);
    await tester.pumpAndSettle();

    expect(find.text('Log Out?'), findsOneWidget);

    // Tap Confirm
    await tester.tap(find.text('Log Out').last);
    await tester.pumpAndSettle();

    expect(mockAuthProvider.logoutCalled, isTrue);

    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });
  });
}
