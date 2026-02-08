import 'package:flutter/material.dart';

import '../widgets/home/home_bottom_nav_bar.dart';
import 'context_report_screen.dart';
import 'saved_listings_screen.dart';
import 'search_screen.dart';
import 'settings_screen.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  int _currentNavIndex = 0;

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
    switch (_currentNavIndex) {
      case 0:
        return const ContextReportScreen();
      case 1:
        return const SearchScreen();
      case 2:
        return const SavedListingsScreen();
      case 3:
        return const SettingsScreen();
      default:
        return const ContextReportScreen();
    }
  }
}
