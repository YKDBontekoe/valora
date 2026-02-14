import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/screens/settings/privacy_security_screen.dart';
import 'package:valora_app/providers/user_profile_provider.dart';
import '../../mocks/mock_user_profile_provider.dart';

void main() {
  testWidgets('PrivacySecurityScreen shows password fields', (WidgetTester tester) async {
    final mockProvider = MockUserProfileProvider();

    await tester.pumpWidget(
      ChangeNotifierProvider<UserProfileProvider>.value(
        value: mockProvider,
        child: const MaterialApp(home: PrivacySecurityScreen()),
      ),
    );

    await tester.pump();

    expect(find.text('Current Password'), findsOneWidget);
    expect(find.text('New Password'), findsOneWidget);
    expect(find.text('Confirm New Password'), findsOneWidget);

    // Clear any pending timers
    await tester.pumpAndSettle();
  });
}
