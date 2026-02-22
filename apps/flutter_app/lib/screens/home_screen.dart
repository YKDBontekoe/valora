import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../providers/workspace_provider.dart';
import '../services/api_service.dart';
import '../services/notification_service.dart';
import '../widgets/home/home_bottom_nav_bar.dart';
import 'context_report_screen.dart';
import 'insights/insights_screen.dart';
import 'notifications_screen.dart';
import 'settings_screen.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  int _currentNavIndex = 0;
  Widget? _contextReportFab;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<NotificationService>().startPolling();
    });
  }

  @override
  Widget build(BuildContext context) {
    return ChangeNotifierProvider<WorkspaceProvider>(
      create: (context) => WorkspaceProvider(
        context.read<ApiService>(),
      ),
      child: Scaffold(
        extendBody: true,
        // Only show the FAB when on the Search tab (index 0)
        floatingActionButton:
            _currentNavIndex == 0 ? _contextReportFab : null,
        bottomNavigationBar: HomeBottomNavBar(
          currentIndex: _currentNavIndex,
          onTap: (index) => setState(() => _currentNavIndex = index),
        ),
        body: _buildBody(),
      ),
    );
  }

  Widget _buildBody() {
    return IndexedStack(
      index: _currentNavIndex,
      children: [
        ContextReportScreen(
          onFabChanged: (fab) {
            if (mounted) {
              setState(() => _contextReportFab = fab);
            }
          },
        ),
        const InsightsScreen(),
        const NotificationsScreen(),
        const SettingsScreen(),
      ],
    );
  }
}
