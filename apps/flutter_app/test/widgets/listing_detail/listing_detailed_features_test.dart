import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/models/listing.dart';
import 'package:valora_app/widgets/listing_detail/listing_detailed_features.dart';

void main() {
  Widget createWidgetUnderTest(Listing listing) {
    return MaterialApp(
      home: Scaffold(
        body: ListingDetailedFeatures(listing: listing),
      ),
    );
  }

  testWidgets('ListingDetailedFeatures renders nothing when features empty', (tester) async {
    final listing = Listing(id: '1', fundaId: '1', address: 'A', features: {});

    await tester.pumpWidget(createWidgetUnderTest(listing));

    expect(find.text('Features'), findsNothing);
  });

  testWidgets('ListingDetailedFeatures renders list of features', (tester) async {
    final listing = Listing(
      id: '1', fundaId: '1', address: 'A',
      features: {
        'Parking': 'Public',
        'Garden': 'Backyard',
      },
    );

    await tester.pumpWidget(createWidgetUnderTest(listing));

    expect(find.text('Features'), findsOneWidget);

    // Find RichText widgets and verify they contain the expected text
    // We expect 5 RichText widgets:
    // 1. "Features" title
    // 2. Icon 1 (Icons are often implemented with RichText/Text under the hood or we might be picking up adjacent text)
    // 3. "Parking: Public"
    // 4. Icon 2
    // 5. "Garden: Backyard"
    final richTextFinder = find.byType(RichText);
    expect(richTextFinder, findsAtLeastNWidgets(2));

    final richTexts = tester.widgetList<RichText>(richTextFinder);

    final parkingText = richTexts.any((widget) {
      final textSpan = widget.text as TextSpan;
      return textSpan.toPlainText().contains('Parking: Public');
    });
    expect(parkingText, isTrue);

    final gardenText = richTexts.any((widget) {
      final textSpan = widget.text as TextSpan;
      return textSpan.toPlainText().contains('Garden: Backyard');
    });
    expect(gardenText, isTrue);

    expect(find.byIcon(Icons.check_circle_outline_rounded), findsNWidgets(2));
  });
}
