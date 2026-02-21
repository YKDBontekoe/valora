import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:provider/provider.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_shadows.dart';
import '../core/theme/valora_typography.dart';
import '../providers/auth_provider.dart';
import 'auth_wrapper.dart';

class StartupScreen extends StatefulWidget {
  const StartupScreen({super.key});

  static const Duration splashDuration = Duration(milliseconds: 1200);

  @override
  State<StartupScreen> createState() => _StartupScreenState();
}

class _StartupScreenState extends State<StartupScreen> {
  final Completer<void> _authCheckCompleter = Completer<void>();

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _checkAuth();
      _startStartupSequence();
    });
  }

  Future<void> _checkAuth() async {
    try {
      await context.read<AuthProvider>().checkAuth();
    } finally {
      if (!_authCheckCompleter.isCompleted) {
        _authCheckCompleter.complete();
      }
    }
  }

  Future<void> _startStartupSequence() async {
    await Future.wait([
      _authCheckCompleter.future,
      Future<void>.delayed(StartupScreen.splashDuration),
    ]);

    if (!mounted) return;
    _navigateToHome();
  }

  void _navigateToHome() {
    Navigator.of(context).pushReplacement(
      PageRouteBuilder(
        pageBuilder: (context, animation, secondaryAnimation) =>
            const AuthWrapper(),
        transitionsBuilder: (context, animation, secondaryAnimation, child) {
          return FadeTransition(opacity: animation, child: child);
        },
        transitionDuration: const Duration(milliseconds: 800),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Theme.of(context).colorScheme.surface,
      body: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            // Animated Icon Container
            Container(
              padding: const EdgeInsets.all(32),
              decoration: BoxDecoration(
                color: ValoraColors.primary.withValues(alpha: 0.1),
                shape: BoxShape.circle,
                boxShadow: ValoraShadows.primary,
              ),
              child: const Icon(
                Icons.home_work_rounded,
                size: 64,
                color: ValoraColors.primary,
              ),
            )
            .animate()
            .scale(
              duration: 600.ms,
              curve: Curves.elasticOut,
              begin: const Offset(0, 0),
              end: const Offset(1, 1),
            )
            .fadeIn(duration: 400.ms),

            const SizedBox(height: 32),

            // Animated Text
            Column(
              children: [
                Text(
                  'Valora',
                  style: ValoraTypography.displayMedium.copyWith(
                    color: ValoraColors.primary,
                    fontWeight: FontWeight.w800,
                    letterSpacing: -0.5,
                  ),
                ),
                const SizedBox(height: 8),
                Text(
                  'Find your dream home',
                  style: ValoraTypography.bodyLarge.copyWith(
                    color: ValoraColors.neutral500,
                    letterSpacing: 0.5,
                  ),
                ),
              ],
            )
            .animate(delay: 200.ms)
            .fadeIn(duration: 600.ms)
            .slideY(begin: 0.2, end: 0, curve: Curves.easeOutCubic, duration: 600.ms),
          ],
        ),
      ),
    );
  }
}
