import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:valora_app/services/pdok_service.dart';
import 'package:valora_app/widgets/search/search_input.dart';
import 'package:valora_app/widgets/valora_widgets.dart';

@GenerateNiceMocks([MockSpec<PdokService>()])
import 'search_input_test.mocks.dart';

void main() {
  late MockPdokService mockPdokService;
  late TextEditingController controller;

  setUp(() {
    mockPdokService = MockPdokService();
    controller = TextEditingController();
  });

  tearDown(() {
    controller.dispose();
  });

  testWidgets('SearchInput renders text field correctly', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: SearchInput(
            controller: controller,
            pdokService: mockPdokService,
            onSuggestionSelected: (_) {},
            onSubmitted: () {},
          ),
        ),
      ),
    );

    expect(find.byType(ValoraTextField), findsOneWidget);
    expect(find.text('City, address, or zip code...'), findsOneWidget);
    expect(find.byIcon(Icons.search_rounded), findsOneWidget);
  });

  testWidgets('SearchInput shows suggestions on text entry', (tester) async {
    final suggestions = [
      PdokSuggestion(
        id: '1',
        displayName: 'Amsterdam',
        type: 'woonplaats',
        score: 1.0,
      ),
      PdokSuggestion(
        id: '2',
        displayName: 'Amstelveen',
        type: 'woonplaats',
        score: 0.9,
      ),
    ];

    when(mockPdokService.search(any)).thenAnswer((_) async => suggestions);

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Column(
            children: [
              SearchInput(
                controller: controller,
                pdokService: mockPdokService,
                onSuggestionSelected: (_) {},
                onSubmitted: () {},
                debounceDuration: Duration.zero,
              ),
              const Expanded(child: SizedBox()),
            ],
          ),
        ),
      ),
    );

    await tester.enterText(find.byType(TextField), 'Ams');
    await tester.pump(); // Trigger callback
    await tester.pumpAndSettle(); // Wait for suggestions to render

    verify(mockPdokService.search('Ams')).called(1);
    expect(find.text('Amsterdam'), findsOneWidget);
    expect(find.text('Amstelveen'), findsOneWidget);
  });

  testWidgets('SearchInput calls onSuggestionSelected when item tapped', (
    tester,
  ) async {
    final suggestions = [
      PdokSuggestion(
        id: '1',
        displayName: 'Amsterdam',
        type: 'woonplaats',
        score: 1.0,
      ),
    ];
    PdokSuggestion? selectedSuggestion;

    when(mockPdokService.search(any)).thenAnswer((_) async => suggestions);

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Column(
            children: [
              SearchInput(
                controller: controller,
                pdokService: mockPdokService,
                onSuggestionSelected: (s) => selectedSuggestion = s,
                onSubmitted: () {},
                debounceDuration: Duration.zero,
              ),
              const Expanded(child: SizedBox()),
            ],
          ),
        ),
      ),
    );

    await tester.enterText(find.byType(TextField), 'Ams');
    await tester.pumpAndSettle();

    await tester.tap(find.text('Amsterdam'));
    await tester.pump();

    expect(selectedSuggestion, isNotNull);
    expect(selectedSuggestion!.displayName, 'Amsterdam');
  });

  testWidgets('SearchInput calls onSubmitted when enter pressed', (tester) async {
    bool submitted = false;

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: SearchInput(
            controller: controller,
            pdokService: mockPdokService,
            onSuggestionSelected: (_) {},
            onSubmitted: () => submitted = true,
          ),
        ),
      ),
    );

    await tester.enterText(find.byType(TextField), 'Test');
    await tester.testTextInput.receiveAction(TextInputAction.search);
    await tester.pump();

    expect(submitted, isTrue);
  });
}
