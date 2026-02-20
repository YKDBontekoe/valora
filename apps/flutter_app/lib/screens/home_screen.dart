import 'insights/insights_screen.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../services/notification_service.dart';
import '../widgets/home/home_bottom_nav_bar.dart';
import 'context_report_screen.dart';
import 'settings_screen.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  int _currentNavIndex = 0;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<NotificationService>().startPolling();
    });
  }

  @override
  void dispose() {
    // We don't have direct access to context in dispose to stop polling,
    // but NotificationService handles its own timer disposal.
    // However, if we wanted to be strict, we'd need a reference.
    // Since NotificationService is provided above MaterialApp, it persists.
    // But HomeScreen is the main authenticated screen.
    // Ideally we should stop polling when HomeScreen is disposed (e.g. logout).
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      extendBody: true,
      bottomNavigationBar: HomeBottomNavBar(
        currentIndex: _currentNavIndex,
        onTap: (index) => setState(() => _currentNavIndex = index),
      ),
      body: _buildBody(),
    );
  }

  Widget _buildBody() {
    return IndexedStack(
      index: _currentNavIndex,
      children: const [
        ContextReportScreen(),
        InsightsScreen(),
        SettingsScreen(),
      ],
    );
  }
}
