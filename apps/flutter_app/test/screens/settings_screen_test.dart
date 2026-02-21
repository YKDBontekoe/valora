import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/providers/auth_provider.dart';
import 'package:valora_app/providers/theme_provider.dart';
import 'package:valora_app/screens/settings_screen.dart';
import 'package:valora_app/screens/notifications_screen.dart';
import 'package:valora_app/services/notification_service.dart';

@GenerateMocks([AuthProvider, ThemeProvider])
@GenerateNiceMocks([
  MockSpec<NotificationService>(),
  MockSpec<HttpClient>(),
  MockSpec<HttpClientRequest>(),
  MockSpec<HttpClientResponse>(),
  MockSpec<HttpHeaders>(),
])
import 'settings_screen_test.mocks.dart';

// Mock HTTP overrides to return a valid 1x1 transparent PNG
class TestHttpOverrides extends HttpOverrides {
  @override
  HttpClient createHttpClient(SecurityContext? context) {
    return _createMockImageHttpClient(context);
  }
}

HttpClient _createMockImageHttpClient(SecurityContext? context) {
  final client = MockHttpClient();
  final request = MockHttpClientRequest();
  final response = MockHttpClientResponse();
  final headers = MockHttpHeaders();

  // Use a catch-all matcher for the URL
  when(client.getUrl(any)).thenAnswer((_) async => request);
  when(request.headers).thenReturn(headers);
  when(request.close()).thenAnswer((_) async => response);
  when(response.contentLength).thenReturn(_transparentImage.length);
  when(response.statusCode).thenReturn(HttpStatus.ok);
  when(
    response.compressionState,
  ).thenReturn(HttpClientResponseCompressionState.notCompressed);
  when(response.listen(any)).thenAnswer((Invocation invocation) {
    final void Function(List<int>) onData = invocation.positionalArguments[0];
    final void Function() onDone = invocation.namedArguments[#onDone];
    final void Function(Object, [StackTrace]) onError =
        invocation.namedArguments[#onError];
    final bool cancelOnError = invocation.namedArguments[#cancelOnError];

    return Stream<List<int>>.fromIterable([_transparentImage]).listen(
      onData,
      onDone: onDone,
      onError: onError,
      cancelOnError: cancelOnError,
    );
  });

  return client;
}

const List<int> _transparentImage = <int>[
  0x89,
  0x50,
  0x4E,
  0x47,
  0x0D,
  0x0A,
  0x1A,
  0x0A,
  0x00,
  0x00,
  0x00,
  0x0D,
  0x49,
  0x48,
  0x44,
  0x52,
  0x00,
  0x00,
  0x00,
  0x01,
  0x00,
  0x00,
  0x00,
  0x01,
  0x08,
  0x06,
  0x00,
  0x00,
  0x00,
  0x1F,
  0x15,
  0xC4,
  0x89,
  0x00,
  0x00,
  0x00,
  0x0A,
  0x49,
  0x44,
  0x41,
  0x54,
  0x78,
  0x9C,
  0x63,
  0x00,
  0x01,
  0x00,
  0x00,
  0x05,
  0x00,
  0x01,
  0x0D,
  0x0A,
  0x2D,
  0xB4,
  0x00,
  0x00,
  0x00,
  0x00,
  0x49,
  0x45,
  0x4E,
  0x44,
  0xAE,
  0x42,
  0x60,
  0x82,
];

void main() {
  late MockAuthProvider mockAuthProvider;
  late MockThemeProvider mockThemeProvider;
  late MockNotificationService mockNotificationService;

  setUp(() {
    mockAuthProvider = MockAuthProvider();
    mockThemeProvider = MockThemeProvider();
    mockNotificationService = MockNotificationService();

    when(mockAuthProvider.email).thenReturn('test@example.com');
    when(mockThemeProvider.isDarkMode).thenReturn(false);
    when(mockNotificationService.unreadCount).thenReturn(0);
    when(mockNotificationService.notifications).thenReturn([]);
    when(mockNotificationService.isLoading).thenReturn(false);
    // NotificationService.error is a concrete getter, so we don't stub it
    // assuming it returns null by default or is not critical for these tests.
    // If needed, we would have to refactor NotificationService to allow overriding.

    // Use the mock HTTP overrides
    HttpOverrides.global = TestHttpOverrides();
  });

  Widget createWidgetUnderTest() {
    return MaterialApp(
      home: const SettingsScreen(),
      builder: (context, child) {
        return MultiProvider(
          providers: [
            ChangeNotifierProvider<AuthProvider>.value(value: mockAuthProvider),
            ChangeNotifierProvider<ThemeProvider>.value(value: mockThemeProvider),
            ChangeNotifierProvider<NotificationService>.value(
              value: mockNotificationService,
            ),
          ],
          child: child ?? const SizedBox(),
        );
      },
    );
  }

  testWidgets('SettingsScreen displays user email', (
    WidgetTester tester,
  ) async {
    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle(); // Wait for animations

    expect(find.text('test@example.com'), findsOneWidget);
  });

  testWidgets('SettingsScreen toggles theme on Appearance tap', (
    WidgetTester tester,
  ) async {
    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle();

    await tester.tap(find.text('Appearance'));
    await tester.pumpAndSettle();

    verify(mockThemeProvider.toggleTheme()).called(1);
  });

  testWidgets('SettingsScreen toggles theme on icon button tap', (
    WidgetTester tester,
  ) async {
    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle();

    await tester.tap(find.byIcon(Icons.dark_mode_rounded));
    await tester.pumpAndSettle();

    verify(mockThemeProvider.toggleTheme()).called(1);
  });

  testWidgets('SettingsScreen handles Profile Edit tap', (
    WidgetTester tester,
  ) async {
    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle();

    await tester.tap(find.text('Edit'));
    await tester.pumpAndSettle();

    expect(find.text('Profile editing coming soon!'), findsOneWidget);
  });

  testWidgets('SettingsScreen interacts with external link tiles', (
    WidgetTester tester,
  ) async {
    // Note: We cannot easily mock launchUrl from url_launcher directly in widget tests
    // without wrapping it in a service or using platform channels.
    // Ideally, we'd use a service wrapper.
    // For now, we just ensure they are tappable and don't crash.
    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle();

    await tester.tap(find.text('Search Preferences'));
    await tester.pumpAndSettle();

    await tester.scrollUntilVisible(
      find.text('Subscription'),
      500.0,
      scrollable: find.byType(Scrollable),
    );
    await tester.tap(find.text('Subscription'));
    await tester.pumpAndSettle();

    await tester.scrollUntilVisible(
      find.text('Privacy & Security'),
      500.0,
      scrollable: find.byType(Scrollable),
    );
    await tester.tap(find.text('Privacy & Security'));
    await tester.pumpAndSettle();
  });

  testWidgets('SettingsScreen navigates to Smart Alerts', (
    WidgetTester tester,
  ) async {
    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle();

    await tester.tap(find.text('Smart Alerts'));
    await tester.pumpAndSettle();

    // Verify navigation occurred (assuming NotificationsScreen has a title 'Notifications')
    // We can just check that SettingsScreen is no longer the top route or that NotificationsScreen is pushed
    // But NotificationsScreen might have scaffold.
    expect(find.byType(NotificationsScreen), findsOneWidget);
  });

  testWidgets('SettingsScreen shows logout confirmation dialog', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(
      1080,
      2400,
    ); // Set large screen to avoid scrolling
    tester.view.devicePixelRatio = 1.0;

    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle(); // Wait for initial animations

    final logoutButtonFinder = find.text('Log Out').last;

    await tester.tap(logoutButtonFinder);
    await tester.pumpAndSettle();

    expect(find.text('Log Out?'), findsOneWidget);
    expect(find.text('Are you sure you want to log out?'), findsOneWidget);

    addTearDown(() => tester.view.resetPhysicalSize());
  });

  testWidgets('SettingsScreen calls logout on confirmation', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 1.0;

    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle(); // Wait for initial animations

    final logoutButtonFinder = find.text('Log Out').last;

    await tester.tap(logoutButtonFinder);
    await tester.pumpAndSettle();

    // Tap confirm in dialog
    final confirmButton = find.widgetWithText(ElevatedButton, 'Log Out');

    await tester.tap(confirmButton);
    await tester.pumpAndSettle();

    verify(mockAuthProvider.logout()).called(1);

    addTearDown(() => tester.view.resetPhysicalSize());
  });

  testWidgets('SettingsScreen cancels logout', (WidgetTester tester) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 1.0;

    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle(); // Wait for initial animations

    final logoutButtonFinder = find.text('Log Out').last;

    await tester.tap(logoutButtonFinder);
    await tester.pumpAndSettle();

    // Verify dialog is open
    expect(find.text('Log Out?'), findsOneWidget);

    // Tap Cancel
    await tester.tap(find.text('Cancel'));
    await tester.pumpAndSettle();

    verifyNever(mockAuthProvider.logout());
    expect(find.text('Log Out?'), findsNothing);

    addTearDown(() => tester.view.resetPhysicalSize());
  });
}
