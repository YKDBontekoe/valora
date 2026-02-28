import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/models/workspace.dart';
import 'package:valora_app/providers/workspace_provider.dart';
import 'package:valora_app/widgets/workspaces/share_workspace_dialog.dart';
import 'package:valora_app/widgets/valora_widgets.dart';

class MockWorkspaceProvider extends ChangeNotifier implements WorkspaceProvider {
  String? invitedEmail;
  WorkspaceRole? invitedRole;
  bool shouldThrow = false;

  @override
  Future<void> inviteMember(String email, WorkspaceRole role) async {
    if (shouldThrow) throw Exception('API Error');
    invitedEmail = email;
    invitedRole = role;
  }

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

void main() {
  late MockWorkspaceProvider mockProvider;

  setUp(() {
    mockProvider = MockWorkspaceProvider();
  });

  Widget createWidgetUnderTest() {
    return MaterialApp(
      home: Scaffold(
        body: Center(
          child: Builder(
            builder: (BuildContext context) {
              return ElevatedButton(
                onPressed: () {
                  showDialog(
                    context: context,
                    builder: (BuildContext dialogContext) {
                      return ChangeNotifierProvider<WorkspaceProvider>.value(
                        value: mockProvider,
                        child: const ShareWorkspaceDialog(),
                      );
                    },
                  );
                },
                child: const Text('Open Dialog'),
              );
            },
          ),
        ),
      ),
    );
  }

  testWidgets('renders dialog correctly', (tester) async {
    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle();

    await tester.tap(find.text('Open Dialog'));
    await tester.pumpAndSettle();

    expect(find.text('Invite Member'), findsOneWidget);
    expect(find.byType(ValoraTextField), findsOneWidget);
    expect(find.byType(DropdownButton<WorkspaceRole>), findsOneWidget);
    expect(find.text('Cancel'), findsOneWidget);
    expect(find.text('Invite'), findsOneWidget);

    await tester.tap(find.text('Cancel'));
    await tester.pumpAndSettle();
  });

  testWidgets('successful invite shows snackbar and calls provider', (tester) async {
    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle();

    await tester.tap(find.text('Open Dialog'));
    await tester.pumpAndSettle();

    final textField = find.byType(ValoraTextField);
    await tester.enterText(textField, 'test@valora.com');
    await tester.pumpAndSettle();

    final inviteButton = find.widgetWithText(ValoraButton, 'Invite');
    await tester.tap(inviteButton, warnIfMissed: false);
    await tester.pumpAndSettle();

    expect(mockProvider.invitedEmail, 'test@valora.com');
    expect(mockProvider.invitedRole, WorkspaceRole.viewer);
    expect(find.text('Member invited successfully'), findsOneWidget);
  });

  testWidgets('failed invite shows error snackbar', (tester) async {
    mockProvider.shouldThrow = true;

    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle();

    await tester.tap(find.text('Open Dialog'));
    await tester.pumpAndSettle();

    final textField = find.byType(ValoraTextField);
    await tester.enterText(textField, 'test@valora.com');
    await tester.pumpAndSettle();

    final inviteButton = find.widgetWithText(ValoraButton, 'Invite');
    await tester.tap(inviteButton, warnIfMissed: false);
    await tester.pumpAndSettle();

    expect(find.textContaining('Failed: Exception: API Error'), findsOneWidget);

    // Dialog should stay open on failure
    expect(find.byType(ShareWorkspaceDialog), findsOneWidget);
  });
}
