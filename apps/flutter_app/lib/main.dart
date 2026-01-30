import 'package:flutter/material.dart';
import 'core/theme/valora_theme.dart';
import 'screens/startup_screen.dart';

void main() {
  runApp(const ValoraApp());
}

class ValoraApp extends StatelessWidget {
  const ValoraApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Valora',
      theme: ValoraTheme.light,
      darkTheme: ValoraTheme.dark,
      themeMode: ThemeMode.system,
      home: const StartupScreen(),
      debugShowCheckedModeBanner: false,
    );
  }
}
