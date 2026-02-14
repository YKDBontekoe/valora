import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/screens/profile/edit_profile_screen.dart';
import 'package:valora_app/providers/user_profile_provider.dart';
import '../../mocks/mock_user_profile_provider.dart';

void main() {
  testWidgets('EditProfileScreen renders fields and saves', (WidgetTester tester) async {
    final mockProvider = MockUserProfileProvider();

    await tester.pumpWidget(
      ChangeNotifierProvider<UserProfileProvider>.value(
        value: mockProvider,
        child: const MaterialApp(home: EditProfileScreen()),
      ),
    );

    expect(find.text('First Name'), findsOneWidget);
    expect(find.text('Last Name'), findsOneWidget);

    await tester.enterText(find.byType(TextField).first, 'John');
    await tester.enterText(find.byType(TextField).last, 'Doe');

    await tester.tap(find.text('Save Changes'));
    await tester.pumpAndSettle();

    expect(mockProvider.profile?.firstName, 'John');
    expect(mockProvider.profile?.lastName, 'Doe');
    await tester.pumpWidget(const SizedBox());
  });
}
