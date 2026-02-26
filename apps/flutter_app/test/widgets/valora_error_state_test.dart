import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/core/exceptions/app_exceptions.dart';
import 'package:valora_app/widgets/valora_error_state.dart';

void main() {
  group('ValoraErrorState', () {
    testWidgets('renders NetworkException correctly', (WidgetTester tester) async {
      final error = NetworkException('No internet');
      await tester.pumpWidget(MaterialApp(
        home: ValoraErrorState(error: error, onRetry: () {}),
      ));
      await tester.pumpAndSettle();

      expect(find.text('No Connection'), findsOneWidget);
      expect(find.text('No internet'), findsOneWidget);
      expect(find.byIcon(Icons.wifi_off_rounded), findsOneWidget);
    });

    testWidgets('renders ServerException correctly', (WidgetTester tester) async {
      final error = ServerException('Server failed');
      await tester.pumpWidget(MaterialApp(
        home: ValoraErrorState(error: error, onRetry: () {}),
      ));
      await tester.pumpAndSettle();

      expect(find.text('Server Error'), findsOneWidget);
      expect(find.text('Server failed'), findsOneWidget);
      expect(find.byIcon(Icons.cloud_off_rounded), findsOneWidget);
    });

    testWidgets('renders NotFoundException correctly', (WidgetTester tester) async {
      final error = NotFoundException('Item missing');
      await tester.pumpWidget(MaterialApp(
        home: ValoraErrorState(error: error, onRetry: () {}),
      ));
      await tester.pumpAndSettle();

      expect(find.text('Not Found'), findsOneWidget);
      expect(find.text('Item missing'), findsOneWidget);
      expect(find.byIcon(Icons.search_off_rounded), findsOneWidget);
    });

    testWidgets('renders ValidationException correctly', (WidgetTester tester) async {
      final error = ValidationException('Bad input');
      await tester.pumpWidget(MaterialApp(
        home: ValoraErrorState(error: error, onRetry: () {}),
      ));
      await tester.pumpAndSettle();

      expect(find.text('Invalid Request'), findsOneWidget);
      expect(find.text('Bad input'), findsOneWidget);
      expect(find.byIcon(Icons.warning_amber_rounded), findsOneWidget);
    });

    testWidgets('renders generic AppException correctly', (WidgetTester tester) async {
      final error = AppException('Something happened');
      await tester.pumpWidget(MaterialApp(
        home: ValoraErrorState(error: error, onRetry: () {}),
      ));
      await tester.pumpAndSettle();

      expect(find.text('Something went wrong'), findsOneWidget);
      expect(find.text('Something happened'), findsOneWidget);
      expect(find.byIcon(Icons.error_outline), findsOneWidget);
    });

    testWidgets('renders fallback message for unknown error', (WidgetTester tester) async {
      final error = Exception('Unknown');
      await tester.pumpWidget(MaterialApp(
        home: ValoraErrorState(error: error, onRetry: () {}),
      ));
      await tester.pumpAndSettle();

      expect(find.text('Something went wrong'), findsOneWidget);
      expect(find.text('An unexpected error occurred. Please try again.'), findsOneWidget);
      expect(find.byIcon(Icons.error_outline), findsOneWidget);
    });

    testWidgets('triggers onRetry callback', (WidgetTester tester) async {
      bool retried = false;
      await tester.pumpWidget(MaterialApp(
        home: ValoraErrorState(
          error: Exception(),
          onRetry: () => retried = true,
        ),
      ));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Try Again'));
      await tester.pumpAndSettle();
      expect(retried, isTrue);
    });
  });
}
