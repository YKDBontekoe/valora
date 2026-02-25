import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../providers/auth_provider.dart';
import '../../widgets/common/valora_text_field.dart';

class RegisterScreen extends StatefulWidget {
  const RegisterScreen({super.key});

  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends State<RegisterScreen> {
  final _formKey = GlobalKey<FormState>();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();
  bool _obscurePassword = true;
  bool _obscureConfirmPassword = true;

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    _confirmPasswordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final authProvider = context.read<AuthProvider>();
    if (authProvider.isLoading) return;
    if (!_formKey.currentState!.validate()) return;

    try {
      await authProvider.register(
        _emailController.text.trim(),
        _passwordController.text,
        _confirmPasswordController.text,
      );
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Account created! Please login.'),
            backgroundColor: ValoraColors.success,
          ),
        );
        Navigator.pop(context); // Go back to Login
      }
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
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final isLoading = context.select<AuthProvider, bool>((p) => p.isLoading);

    return Scaffold(
      backgroundColor: isDark
          ? ValoraColors.backgroundDark
          : ValoraColors.backgroundLight,
      body: SafeArea(
        child: Center(
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
                      Center(
                        child: Container(
                          width: 64,
                          height: 64,
                          decoration: BoxDecoration(
                            color: ValoraColors.primary.withValues(alpha: 0.1),
                            borderRadius: BorderRadius.circular(16),
                          ),
                          child: const Icon(
                            Icons.person_add_alt_rounded,
                            size: 32,
                            color: ValoraColors.primary,
                          ),
                        ),
                      ),
                      const SizedBox(height: 24),
                      Text(
                        'Create Account',
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
                        'Join Valora to find your dream home',
                        textAlign: TextAlign.center,
                        style: ValoraTypography.bodyMedium.copyWith(
                          color: isDark
                              ? ValoraColors.neutral400
                              : ValoraColors.neutral500,
                        ),
                      ),
                      const SizedBox(height: 32),
                      ValoraTextField(
                        controller: _emailController,
                        label: 'Email',
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
                            return 'Required';
                          }
                          if (!value.contains('@')) {
                            return 'Invalid email';
                          }
                          return null;
                        },
                      ),
                      const SizedBox(height: 20),
                      ValoraTextField(
                        key: const Key('password_field'),
                        controller: _passwordController,
                        label: 'Password',
                        hint: '••••••••',
                        prefixIcon: Icon(
                          Icons.lock_outline,
                          size: 20,
                          color: isDark
                              ? ValoraColors.neutral500
                              : ValoraColors.neutral400,
                        ),
                        obscureText: _obscurePassword,
                        keyboardType: TextInputType.visiblePassword,
                        autofillHints: const [AutofillHints.newPassword],
                        suffixIcon: IconButton(
                          tooltip: _obscurePassword
                              ? 'Show password'
                              : 'Hide password',
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
                        textInputAction: TextInputAction.next,
                        validator: (value) {
                          if (value == null || value.isEmpty) {
                            return 'Required';
                          }
                          if (value.length < 6) {
                            return 'Min 6 characters';
                          }
                          return null;
                        },
                      ),
                      const SizedBox(height: 20),
                      ValoraTextField(
                        key: const Key('confirm_password_field'),
                        controller: _confirmPasswordController,
                        label: 'Confirm Password',
                        hint: '••••••••',
                        prefixIcon: Icon(
                          Icons.lock_outline,
                          size: 20,
                          color: isDark
                              ? ValoraColors.neutral500
                              : ValoraColors.neutral400,
                        ),
                        obscureText: _obscureConfirmPassword,
                        keyboardType: TextInputType.visiblePassword,
                        autofillHints: const [AutofillHints.newPassword],
                        suffixIcon: IconButton(
                          tooltip: _obscureConfirmPassword
                              ? 'Show password'
                              : 'Hide password',
                          icon: Icon(
                            _obscureConfirmPassword
                                ? Icons.visibility_off_outlined
                                : Icons.visibility_outlined,
                            size: 20,
                            color: isDark
                                ? ValoraColors.neutral400
                                : ValoraColors.neutral500,
                          ),
                          onPressed: () {
                            setState(() {
                              _obscureConfirmPassword =
                                  !_obscureConfirmPassword;
                            });
                          },
                        ),
                        textInputAction: TextInputAction.done,
                        onSubmitted:
                            isLoading ? null : (_) => _submit(),
                        validator: (value) {
                          if (value == null || value.isEmpty) {
                            return 'Required';
                          }
                          if (value != _passwordController.text) {
                            return 'Passwords do not match';
                          }
                          return null;
                        },
                      ),
                      const SizedBox(height: 32),
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
                              : const Text('Register'),
                        ),
                      ),
                      const SizedBox(height: 24),
                      Wrap(
                        alignment: WrapAlignment.center,
                        crossAxisAlignment: WrapCrossAlignment.center,
                        children: [
                          Text(
                            'Already have an account? ',
                            style: ValoraTypography.bodyMedium.copyWith(
                              color: isDark
                                  ? ValoraColors.neutral400
                                  : ValoraColors.neutral500,
                            ),
                          ),
                          TextButton(
                            onPressed: () => Navigator.pop(context),
                            child: Text(
                              'Login',
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
      ),
    );
  }
}
