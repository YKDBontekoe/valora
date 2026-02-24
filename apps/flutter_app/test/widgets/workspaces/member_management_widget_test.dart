import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/widgets/workspaces/member_management_widget.dart';
import 'package:valora_app/models/workspace.dart';
import 'package:valora_app/widgets/common/valora_empty_state.dart';

void main() {
  testWidgets('MemberManagementWidget renders empty state', (WidgetTester tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: const MemberManagementWidget(members: [], canInvite: true),
        ),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.byType(ValoraEmptyState), findsOneWidget);
    expect(find.text('No members yet'), findsOneWidget);
  });

  testWidgets('MemberManagementWidget renders members correctly', (WidgetTester tester) async {
    final members = [
      WorkspaceMember(
        id: 'm1',
        userId: 'u1',
        email: 'user1@example.com',
        role: WorkspaceRole.owner, // Fixed: admin -> owner
        joinedAt: DateTime.now(),
        isPending: false,
      ),
      WorkspaceMember(
        id: 'm2',
        userId: 'u2',
        email: 'user2@example.com',
        role: WorkspaceRole.viewer,
        joinedAt: DateTime.now(),
        isPending: true,
      ),
    ];

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: MemberManagementWidget(members: members, canInvite: false),
        ),
      ),
    );

    // Initial pump
    await tester.pump();

    // Pump and settle for animations inside ValoraCard and ValoraBadge
    await tester.pumpAndSettle();

    expect(find.text('user1@example.com'), findsOneWidget);
    expect(find.text('OWNER'), findsOneWidget); // Fixed: ADMIN -> OWNER
    expect(find.text('PENDING'), findsOneWidget);

    // Check for role text
    expect(find.text('VIEWER'), findsOneWidget);

    // Check that invite button is NOT present when canInvite is false
    expect(find.byIcon(Icons.person_add_rounded), findsNothing);
  });
}
