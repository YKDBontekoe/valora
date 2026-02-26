import 'dart:async';

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/auth_provider.dart';
import 'auth_wrapper.dart';

class StartupScreen extends StatefulWidget {
  const StartupScreen({super.key});

  @override
  State<StartupScreen> createState() => _StartupScreenState();
}

class _StartupScreenState extends State<StartupScreen>
    with SingleTickerProviderStateMixin {
  static const Duration _animatedSplashDuration = Duration(milliseconds: 900);
  static const Duration _minimumStartupDuration = Duration(milliseconds: 500);

  late AnimationController _controller;
  late Animation<double> _iconScaleAnimation;
  late Animation<double> _fadeAnimation;
  late Animation<Offset> _textSlideAnimation;
  late bool _disableAnimations;

  @override
  void initState() {
    super.initState();

    _disableAnimations = WidgetsBinding
        .instance
        .platformDispatcher
        .accessibilityFeatures
        .disableAnimations;

    _controller = AnimationController(
      vsync: this,
      duration: _disableAnimations ? Duration.zero : _animatedSplashDuration,
    );

    // Icon pops in with an elastic effect
    _iconScaleAnimation = Tween<double>(begin: 0.0, end: 1.0).animate(
      CurvedAnimation(
        parent: _controller,
        curve: const Interval(0.0, 0.6, curve: Curves.elasticOut),
      ),
    );

    // Text and Icon fade in together
    _fadeAnimation = Tween<double>(begin: 0.0, end: 1.0).animate(
      CurvedAnimation(
        parent: _controller,
        curve: const Interval(0.2, 0.6, curve: Curves.easeIn),
      ),
    );

    // Text slides up gently
    _textSlideAnimation =
        Tween<Offset>(begin: const Offset(0, 0.2), end: Offset.zero).animate(
          CurvedAnimation(
            parent: _controller,
            curve: const Interval(0.3, 0.8, curve: Curves.easeOutCubic),
          ),
        );

    _startStartupSequence();
  }

  Future<void> _startStartupSequence() async {
    try {
      Future<void>? animationFuture;
      if (!_disableAnimations) {
        animationFuture = _controller.forward().orCancel;
      } else {
        _controller.value = 1;
      }

      // Allow initial build to complete before triggering auth check
      // (which might notify listeners and cause rebuilds)
      await Future.delayed(Duration.zero);
      if (!mounted) return;

      final authFuture = context.read<AuthProvider>().checkAuth();
      final minDurationFuture = Future.delayed(_minimumStartupDuration);

      await Future.wait([
        authFuture,
        minDurationFuture,
        if (animationFuture != null) animationFuture,
      ]);

      if (!mounted) return;
      _navigateToHome();
    } on TickerCanceled {
      // Animation canceled because the widget was disposed.
    } catch (e) {
      // On any other error, try to proceed to home (which will redirect to login if auth failed)
      if (mounted) _navigateToHome();
    }
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
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colorScheme = theme.colorScheme;

    return Scaffold(
      backgroundColor: colorScheme.surface,
      body: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            // Animated Icon Container
            ScaleTransition(
              scale: _iconScaleAnimation,
              child: Container(
                padding: const EdgeInsets.all(32),
                decoration: BoxDecoration(
                  color: colorScheme.primaryContainer,
                  shape: BoxShape.circle,
                  boxShadow: [
                    BoxShadow(
                      color: colorScheme.primary.withValues(alpha: 0.2),
                      blurRadius: 20,
                      offset: const Offset(0, 10),
                    ),
                  ],
                ),
                child: Icon(
                  Icons.home_work_rounded,
                  size: 64,
                  color: colorScheme.primary,
                ),
              ),
            ),
            const SizedBox(height: 32),
            // Animated Text
            SlideTransition(
              position: _textSlideAnimation,
              child: FadeTransition(
                opacity: _fadeAnimation,
                child: Column(
                  children: [
                    Text(
                      'Valora',
                      style: theme.textTheme.displayMedium?.copyWith(
                        color: colorScheme.primary,
                        fontWeight: FontWeight.w800,
                        letterSpacing: -0.5,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Text(
                      'Find your dream home',
                      style: theme.textTheme.bodyLarge?.copyWith(
                        color: colorScheme.onSurfaceVariant,
                        letterSpacing: 0.5,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
