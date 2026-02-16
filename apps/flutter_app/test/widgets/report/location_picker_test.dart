import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:valora_app/widgets/report/location_picker.dart';

void main() {
  testWidgets('LocationPicker renders map and instructions', (tester) async {
    await tester.pumpWidget(const MaterialApp(
      home: LocationPicker(),
    ));

    expect(find.byType(FlutterMap), findsOneWidget);
    expect(find.text('Tap on the map to select a location'), findsOneWidget);
  });
}
