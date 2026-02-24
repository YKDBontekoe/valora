import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/models/saved_search.dart';
import 'package:valora_app/providers/context_report_provider.dart';
import 'package:valora_app/screens/context_report/widgets/saved_searches_section.dart';

// Minimal mock provider using ChangeNotifier
class MockContextReportProvider extends ChangeNotifier implements ContextReportProvider {
  List<SavedSearch> _savedSearches = [];
  final bool _isLoading = false; // Fixed: Made final to satisfy linter

  @override
  List<SavedSearch> get savedSearches => _savedSearches;

  @override
  bool get isLoading => _isLoading;

  void addSavedSearch(SavedSearch search) {
    _savedSearches = List.from(_savedSearches)..add(search);
    notifyListeners();
  }

  @override
  Future<void> toggleSearchAlert(String id) async {
    final index = _savedSearches.indexWhere((s) => s.id == id);
    if (index != -1) {
      final s = _savedSearches[index];
      _savedSearches = List.from(_savedSearches);
      _savedSearches[index] = s.copyWith(isAlertEnabled: !s.isAlertEnabled);
      notifyListeners();
    }
  }

  @override
  Future<void> removeSavedSearch(String id) async {
    _savedSearches = List.from(_savedSearches)..removeWhere((s) => s.id == id);
    notifyListeners();
  }

  @override
  Future<void> generate(String input) async {}

  @override
  void setRadiusMeters(int value) {}

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

void main() {
  testWidgets('SavedSearchesSection renders and interacts', (WidgetTester tester) async {
    final provider = MockContextReportProvider();
    final controller = TextEditingController();

    provider.addSavedSearch(SavedSearch(
      id: '1',
      query: 'Amsterdam',
      radiusMeters: 1000,
      createdAt: DateTime.now(),
      isAlertEnabled: false,
    ));

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: ChangeNotifierProvider<ContextReportProvider>.value(
            value: provider,
            child: SavedSearchesSection(
              controller: controller,
              provider: provider,
            ),
          ),
        ),
      ),
    );

    expect(find.text('Amsterdam'), findsOneWidget);
    expect(find.byIcon(Icons.notifications_none_rounded), findsOneWidget);
    expect(find.byIcon(Icons.close_rounded), findsOneWidget); // Verify close button exists initially

    // Toggle alert
    await tester.tap(find.byIcon(Icons.notifications_none_rounded));
    await tester.pump();

    expect(provider.savedSearches.first.isAlertEnabled, true);
    expect(find.byIcon(Icons.notifications_active_rounded), findsOneWidget);

    // Delete
    await tester.tap(find.byIcon(Icons.close_rounded));
    await tester.pumpAndSettle(); // Dialog

    expect(find.text('Remove Saved Search?'), findsOneWidget);

    await tester.tap(find.text('Remove'));
    await tester.pumpAndSettle();

    expect(provider.savedSearches.isEmpty, true);
    expect(find.text('Amsterdam'), findsNothing);
  });
}
