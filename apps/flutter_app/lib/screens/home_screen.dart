import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../services/notification_service.dart';
import '../widgets/home/home_bottom_nav_bar.dart';
import 'context_report_screen.dart';
import 'insights/insights_screen.dart';
import 'saved_properties_screen.dart';
import 'settings_screen.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => HomeScreenState();
}

class HomeScreenState extends State<HomeScreen> {
  int _currentNavIndex = 0;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<NotificationService>().startPolling();
    });
  }

  void switchToTab(int index) {
    setState(() {
      _currentNavIndex = index;
    });
  }

  @override
  void dispose() {
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
        ContextReportScreen(), // Index 0: Search
        InsightsScreen(),      // Index 1: Insights
        ContextReportScreen(), // Index 2: Report (Same as Search for now)
        SavedPropertiesScreen(), // Index 3: Saved
        SettingsScreen(),      // Index 4: Settings
      ],
    );
  }
}
