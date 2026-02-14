import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/screens/settings/search_preferences_screen.dart';
import 'package:valora_app/providers/user_profile_provider.dart';
import '../../mocks/mock_user_profile_provider.dart';

void main() {
  testWidgets('SearchPreferencesScreen displays slider and saves', (WidgetTester tester) async {
    final mockProvider = MockUserProfileProvider();

    await tester.pumpWidget(
      ChangeNotifierProvider<UserProfileProvider>.value(
        value: mockProvider,
        child: const MaterialApp(home: SearchPreferencesScreen()),
      ),
    );

    expect(find.byType(Slider), findsOneWidget);
    expect(find.text('1000m'), findsOneWidget);

    await tester.drag(find.byType(Slider), const Offset(100, 0));
    await tester.pump();

    await tester.tap(find.text('Save Preferences'));
    await tester.pumpAndSettle();

    expect(mockProvider.profile?.defaultRadiusMeters, isNot(1000));
    await tester.pumpWidget(const SizedBox());
  });
}
