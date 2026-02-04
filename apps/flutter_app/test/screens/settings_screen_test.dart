import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/providers/auth_provider.dart';
import 'package:valora_app/providers/theme_provider.dart';
import 'package:valora_app/screens/settings_screen.dart';
import 'package:valora_app/services/api_service.dart';

@GenerateMocks([AuthProvider, ThemeProvider, ApiService])
@GenerateNiceMocks([
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
  when(response.compressionState).thenReturn(HttpClientResponseCompressionState.notCompressed);
  when(response.listen(any)).thenAnswer((Invocation invocation) {
    final void Function(List<int>) onData = invocation.positionalArguments[0];
    final void Function() onDone = invocation.namedArguments[#onDone];
    final void Function(Object, [StackTrace]) onError = invocation.namedArguments[#onError];
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
  0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49,
  0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x06,
  0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4, 0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44,
  0x41, 0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00, 0x05, 0x00, 0x01, 0x0D,
  0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42,
  0x60, 0x82,
];

void main() {
  late MockAuthProvider mockAuthProvider;
  late MockApiService mockApiService;
  late MockThemeProvider mockThemeProvider;

  setUp(() {
    mockAuthProvider = MockAuthProvider();
    mockThemeProvider = MockThemeProvider();
    mockApiService = MockApiService();

    when(mockAuthProvider.email).thenReturn('test@example.com');
    when(mockThemeProvider.isDarkMode).thenReturn(false);
    when(mockAuthProvider.isAdmin).thenReturn(false);

    // Use the mock HTTP overrides
    HttpOverrides.global = TestHttpOverrides();
  });

  Widget createWidgetUnderTest() {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider<AuthProvider>.value(value: mockAuthProvider),
        ChangeNotifierProvider<ThemeProvider>.value(value: mockThemeProvider),
        Provider<ApiService>.value(value: mockApiService),
      ],
      child: const MaterialApp(
        home: SettingsScreen(),
      ),
    );
  }

  testWidgets('SettingsScreen displays user email', (WidgetTester tester) async {
    await tester.pumpWidget(createWidgetUnderTest());

    expect(find.text('test@example.com'), findsOneWidget);
  });

  testWidgets('SettingsScreen shows logout confirmation dialog', (WidgetTester tester) async {
    tester.view.physicalSize = const Size(1080, 2400); // Set large screen to avoid scrolling
    tester.view.devicePixelRatio = 1.0;

    await tester.pumpWidget(createWidgetUnderTest());

    final logoutButtonFinder = find.text('Log Out').last;

    await tester.tap(logoutButtonFinder);
    await tester.pumpAndSettle();

    expect(find.text('Log Out?'), findsOneWidget);
    expect(find.text('Are you sure you want to log out?'), findsOneWidget);

    addTearDown(() => tester.view.resetPhysicalSize());
  });

  testWidgets('SettingsScreen calls logout on confirmation', (WidgetTester tester) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 1.0;

    await tester.pumpWidget(createWidgetUnderTest());

    final logoutButtonFinder = find.text('Log Out').last;

    await tester.tap(logoutButtonFinder);
    await tester.pumpAndSettle();

    // Tap confirm in dialog
    // The dialog title is 'Log Out?' (with question mark)
    // The button is 'Log Out' (no question mark)
    // Since ValoraButton might wrap ElevatedButton, look for that.
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

  testWidgets('SettingsScreen shows admin controls when user is admin', (WidgetTester tester) async {
    when(mockAuthProvider.isAdmin).thenReturn(true);

    await tester.pumpWidget(createWidgetUnderTest());

    expect(find.text('ADMIN CONTROLS'), findsOneWidget);
    expect(find.text('Trigger Scrape'), findsOneWidget);
  });

  testWidgets('SettingsScreen hides admin controls when user is not admin', (WidgetTester tester) async {
    when(mockAuthProvider.isAdmin).thenReturn(false);

    await tester.pumpWidget(createWidgetUnderTest());

    expect(find.text('ADMIN CONTROLS'), findsNothing);
  });

  testWidgets('Admin can trigger scrape', (WidgetTester tester) async {
    when(mockAuthProvider.isAdmin).thenReturn(true);

    await tester.pumpWidget(createWidgetUnderTest());

    // Use scrollUntilVisible if needed, but here we assume it fits or use a large screen
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 1.0;

    await tester.tap(find.text('Trigger Scrape'));
    await tester.pump(); // Start future
    await tester.pump(); // Process future

    verify(mockApiService.triggerScrape()).called(1);
    addTearDown(() => tester.view.resetPhysicalSize());
  });

  testWidgets('Admin can trigger limited scrape', (WidgetTester tester) async {
    when(mockAuthProvider.isAdmin).thenReturn(true);

    await tester.pumpWidget(createWidgetUnderTest());
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 1.0;

    await tester.tap(find.text('Limited Scrape'));
    await tester.pumpAndSettle();

    // Fill dialog
    await tester.enterText(find.widgetWithText(TextField, 'Region (e.g. amsterdam)'), 'utrecht');
    await tester.enterText(find.widgetWithText(TextField, 'Limit'), '5');

    await tester.tap(find.widgetWithText(ElevatedButton, 'Trigger'));
    await tester.pumpAndSettle();

    verify(mockApiService.triggerLimitedScrape('utrecht', 5)).called(1);
    addTearDown(() => tester.view.resetPhysicalSize());
  });

  testWidgets('Admin can trigger seed database', (WidgetTester tester) async {
    when(mockAuthProvider.isAdmin).thenReturn(true);

    await tester.pumpWidget(createWidgetUnderTest());
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 1.0;

    await tester.tap(find.text('Seed Database'));
    await tester.pumpAndSettle();

    await tester.enterText(find.widgetWithText(TextField, 'Region (e.g. amsterdam)'), 'rotterdam');

    await tester.tap(find.widgetWithText(ElevatedButton, 'Seed'));
    await tester.pumpAndSettle();

    verify(mockApiService.seedDatabase('rotterdam')).called(1);
    addTearDown(() => tester.view.resetPhysicalSize());
  });

  testWidgets('Admin can clear database', (WidgetTester tester) async {
    when(mockAuthProvider.isAdmin).thenReturn(true);

    await tester.pumpWidget(createWidgetUnderTest());
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 1.0;

    await tester.tap(find.text('Clear Database'));
    await tester.pumpAndSettle();

    expect(find.text('Clear Database?'), findsOneWidget);

    await tester.tap(find.widgetWithText(ElevatedButton, 'Clear All'));
    await tester.pumpAndSettle();

    verify(mockApiService.clearDatabase()).called(1);
    addTearDown(() => tester.view.resetPhysicalSize());
  });
}
