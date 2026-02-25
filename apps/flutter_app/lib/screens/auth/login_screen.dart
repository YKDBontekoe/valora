import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:url_launcher/url_launcher.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../providers/auth_provider.dart';
import '../../widgets/common/valora_text_field.dart';
import '../../widgets/auth/social_login_button.dart';
import 'register_screen.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _obscurePassword = true;

  Future<void> _openForgotPassword() async {
    final Uri uri = Uri(
      scheme: 'mailto',
      path: 'support@valora.nl',
      queryParameters: <String, String>{'subject': 'Password reset request'},
    );

    if (!await launchUrl(uri)) {
      if (!mounted) {
        return;
      }
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Unable to open email client right now.'),
          backgroundColor: ValoraColors.error,
        ),
      );
    }
  }

  Future<void> _openSocialProvider(String provider) async {
    if (provider.toLowerCase() == 'google') {
      try {
        await context.read<AuthProvider>().loginWithGoogle();
      } catch (e) {
        if (!mounted) return;
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Google Login failed: $e')),
        );
      }
      return;
    }

    final Uri uri = Uri.parse('https://valora.nl/auth/$provider');
    if (!await launchUrl(uri, mode: LaunchMode.externalApplication)) {
      if (!mounted) {
        return;
      }
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Could not launch social login')),
      );
    }
  }

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;

    final authProvider = context.read<AuthProvider>();
    if (authProvider.isLoading) return;

    try {
      await authProvider.login(
        _emailController.text.trim(),
        _passwordController.text,
      );
      // Navigation is handled by AuthWrapper via state change
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(e.toString().replaceAll('Exception: ', '')),
            backgroundColor: ValoraColors.error,
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final isLoading = context.select<AuthProvider, bool>((p) => p.isLoading);
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Scaffold(
      backgroundColor: isDark
          ? ValoraColors.backgroundDark
          : ValoraColors.backgroundLight,
      body: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(ValoraSpacing.screenPadding),
          child: Container(
            constraints: const BoxConstraints(maxWidth: 450),
            decoration: BoxDecoration(
              color: isDark ? ValoraColors.surfaceDark : Colors.white,
              borderRadius: BorderRadius.circular(32),
              boxShadow: [
                BoxShadow(
                  color: Colors.black.withValues(alpha: 0.1),
                  blurRadius: 50,
                  offset: const Offset(0, 25),
                  spreadRadius: -12,
                ),
              ],
              border: Border.all(
                color: isDark
                    ? Colors.white.withValues(alpha: 0.1)
                    : Colors.white.withValues(alpha: 0.5),
                width: 1,
              ),
            ),
            padding: const EdgeInsets.all(32),
            child: Form(
              key: _formKey,
              child: AutofillGroup(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    // Icon
                    Center(
                      child: Container(
                        width: 64,
                        height: 64,
                        decoration: BoxDecoration(
                          color: ValoraColors.primary.withValues(alpha: 0.1),
                          borderRadius: BorderRadius.circular(16),
                        ),
                        child: const Icon(
                          Icons.home_rounded,
                          size: 32,
                          color: ValoraColors.primary,
                        ),
                      ),
                    ),
                    const SizedBox(height: 24),

                    // Title
                    Text(
                      'Welcome Back',
                      textAlign: TextAlign.center,
                      style: ValoraTypography.headlineMedium.copyWith(
                        fontWeight: FontWeight.bold,
                        color: isDark
                            ? ValoraColors.onSurfaceDark
                            : ValoraColors.onSurfaceLight,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Text(
                      'Sign in to access your premium real estate insights',
                      textAlign: TextAlign.center,
                      style: ValoraTypography.bodyMedium.copyWith(
                        color: isDark
                            ? ValoraColors.neutral400
                            : ValoraColors.neutral500,
                      ),
                    ),
                    const SizedBox(height: 32),

                    // Email Field
                    ValoraTextField(
                      controller: _emailController,
                      label: 'Email Address',
                      hint: 'hello@example.com',
                      prefixIcon: Icon(
                        Icons.email_outlined,
                        size: 20,
                        color: isDark
                            ? ValoraColors.neutral500
                            : ValoraColors.neutral400,
                      ),
                      keyboardType: TextInputType.emailAddress,
                      autofillHints: const [AutofillHints.email],
                      validator: (value) {
                        if (value == null || value.isEmpty) {
                          return 'Please enter your email';
                        }
                        if (!value.contains('@')) {
                          return 'Please enter a valid email';
                        }
                        return null;
                      },
                    ),
                    const SizedBox(height: 20),

                    // Password Field
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Padding(
                          padding: const EdgeInsets.only(left: 4),
                          child: Text(
                            'Password',
                            style: ValoraTypography.labelSmall.copyWith(
                              fontWeight: FontWeight.w600,
                              color: isDark
                                  ? ValoraColors.neutral400
                                  : ValoraColors.neutral500,
                            ),
                          ),
                        ),
                        TextButton(
                          onPressed: _openForgotPassword,
                          style: TextButton.styleFrom(
                            tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                            padding: const EdgeInsets.symmetric(
                              horizontal: 6,
                              vertical: 4,
                            ),
                          ),
                          child: Text(
                            'Forgot?',
                            style: ValoraTypography.labelSmall.copyWith(
                              color: ValoraColors.primary,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: ValoraSpacing.xs),
                    ValoraTextField(
                      controller: _passwordController,
                      hint: '••••••••',
                      prefixIcon: Icon(
                        Icons.lock_outline,
                        size: 20,
                        color: isDark
                            ? ValoraColors.neutral500
                            : ValoraColors.neutral400,
                      ),
                      obscureText: _obscurePassword,
                      autofillHints: const [AutofillHints.password],
                      suffixIcon: IconButton(
                        icon: Icon(
                          _obscurePassword
                              ? Icons.visibility_off_outlined
                              : Icons.visibility_outlined,
                          size: 20,
                          color: isDark
                              ? ValoraColors.neutral400
                              : ValoraColors.neutral500,
                        ),
                        onPressed: () {
                          setState(() {
                            _obscurePassword = !_obscurePassword;
                          });
                        },
                      ),
                      validator: (value) {
                        if (value == null || value.isEmpty) {
                          return 'Please enter your password';
                        }
                        return null;
                      },
                      onSubmitted: (_) => _submit(),
                    ),
                    const SizedBox(height: 32),

                    // Login Button
                    SizedBox(
                      width: double.infinity,
                      height: 54,
                      child: ElevatedButton(
                        onPressed: isLoading ? null : _submit,
                        style: ElevatedButton.styleFrom(
                          backgroundColor: ValoraColors.primary,
                          foregroundColor: Colors.white,
                          elevation: 10,
                          shadowColor: ValoraColors.primary.withValues(
                            alpha: 0.3,
                          ),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(16),
                          ),
                          textStyle: ValoraTypography.labelLarge.copyWith(
                            fontWeight: FontWeight.w600,
                            fontSize: 16,
                          ),
                        ),
                        child: isLoading
                            ? const SizedBox(
                                height: 24,
                                width: 24,
                                child: CircularProgressIndicator(
                                  color: Colors.white,
                                  strokeWidth: 2.5,
                                ),
                              )
                            : const Text('Login'),
                      ),
                    ),

                    // Divider
                    const SizedBox(height: 24),
                    Row(
                      children: [
                        Expanded(
                          child: Divider(
                            color: isDark
                                ? ValoraColors.neutral700
                                : ValoraColors.neutral200,
                          ),
                        ),
                        Padding(
                          padding: const EdgeInsets.symmetric(horizontal: 16),
                          child: Text(
                            'Or continue with',
                            style: ValoraTypography.labelSmall.copyWith(
                              color: isDark
                                  ? ValoraColors.neutral400
                                  : ValoraColors.neutral500,
                            ),
                          ),
                        ),
                        Expanded(
                          child: Divider(
                            color: isDark
                                ? ValoraColors.neutral700
                                : ValoraColors.neutral200,
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 24),

                    // Social Buttons
                    Row(
                      children: [
                        Expanded(
                          child: SocialLoginButton(
                            label: 'Google',
                            icon: Icons.g_mobiledata,
                            onTap: () => _openSocialProvider('google'),
                          ),
                        ),
                        const SizedBox(width: 16),
                        Expanded(
                          child: SocialLoginButton(
                            label: 'Apple',
                            icon: Icons.apple,
                            onTap: () => _openSocialProvider('apple'),
                          ),
                        ),
                      ],
                    ),

                    // Footer
                    const SizedBox(height: 32),
                    Wrap(
                      alignment: WrapAlignment.center,
                      crossAxisAlignment: WrapCrossAlignment.center,
                      children: [
                        Text(
                          'Don\'t have an account? ',
                          style: ValoraTypography.bodyMedium.copyWith(
                            color: isDark
                                ? ValoraColors.neutral400
                                : ValoraColors.neutral500,
                          ),
                        ),
                        TextButton(
                          onPressed: () {
                            Navigator.push(
                              context,
                              MaterialPageRoute(
                                builder: (context) => const RegisterScreen(),
                              ),
                            );
                          },
                          style: TextButton.styleFrom(
                            tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                            padding: const EdgeInsets.symmetric(
                              horizontal: 6,
                              vertical: 4,
                            ),
                          ),
                          child: Text(
                            'Create Account',
                            style: ValoraTypography.bodyMedium.copyWith(
                              color: ValoraColors.primary,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
