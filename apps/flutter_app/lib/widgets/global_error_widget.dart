import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../core/theme/valora_spacing.dart';
import '../core/theme/valora_typography.dart';
import '../screens/startup_screen.dart';

class GlobalErrorWidget extends StatelessWidget {
  final FlutterErrorDetails details;

  const GlobalErrorWidget({super.key, required this.details});

  @override
  Widget build(BuildContext context) {
    final ColorScheme colorScheme = Theme.of(context).colorScheme;

    return Scaffold(
      backgroundColor: colorScheme.surface,
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(ValoraSpacing.xl),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(
                Icons.error_outline_rounded,
                size: 64,
                color: colorScheme.error,
              ),
              const SizedBox(height: ValoraSpacing.lg),
              Text(
                "We're sorry, something went wrong",
                style: ValoraTypography.headlineSmall.copyWith(
                  color: colorScheme.onSurface,
                  fontWeight: FontWeight.bold,
                ),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: ValoraSpacing.sm),
              Text(
                'Please restart the application. If the problem persists, contact support.',
                style: ValoraTypography.bodyMedium.copyWith(
                  color: colorScheme.onSurfaceVariant,
                ),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: ValoraSpacing.xl),
              Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  ElevatedButton.icon(
                    onPressed: () {
                      Navigator.maybeOf(context)?.pushAndRemoveUntil(
                        MaterialPageRoute(
                          builder: (context) => const StartupScreen(),
                        ),
                        (route) => false,
                      );
                    },
                    icon: const Icon(Icons.refresh_rounded),
                    label: const Text('Restart'),
                  ),
                  const SizedBox(width: ValoraSpacing.md),
                  if (kDebugMode)
                    OutlinedButton.icon(
                      onPressed: () async {
                        try {
                          await Clipboard.setData(
                            ClipboardData(text: details.exception.toString()),
                          );
                          if (context.mounted) {
                            ScaffoldMessenger.maybeOf(context)?.showSnackBar(
                              const SnackBar(
                                content: Text('Error copied to clipboard'),
                              ),
                            );
                          }
                        } catch (e) {
                          if (context.mounted) {
                            ScaffoldMessenger.maybeOf(context)?.showSnackBar(
                              const SnackBar(
                                content: Text('Failed to copy error'),
                              ),
                            );
                          }
                        }
                      },
                      icon: const Icon(Icons.copy_rounded),
                      label: const Text('Copy Error'),
                      style: OutlinedButton.styleFrom(
                        foregroundColor: colorScheme.error,
                        side: BorderSide(color: colorScheme.error),
                        padding: const EdgeInsets.symmetric(
                          horizontal: 24,
                          vertical: 12,
                        ),
                      ),
                    ),
                ],
              ),
              const SizedBox(height: ValoraSpacing.xl),
              if (kDebugMode) ...[
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: colorScheme.surfaceContainerHighest,
                    borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
                    border: Border.all(color: colorScheme.outlineVariant),
                  ),
                  height: 200,
                  width: double.infinity,
                  child: SingleChildScrollView(
                    child: Text(
                      details.exception.toString(),
                      style: TextStyle(
                        fontFamily: 'Courier',
                        fontSize: 12,
                        color: colorScheme.error,
                      ),
                    ),
                  ),
                ),
                const SizedBox(height: ValoraSpacing.lg),
              ],
            ],
          ),
        ),
      ),
    );
  }
}
