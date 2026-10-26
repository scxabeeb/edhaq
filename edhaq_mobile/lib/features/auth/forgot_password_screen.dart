import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:fluttertoast/fluttertoast.dart';
import 'package:go_router/go_router.dart';
import '../../core/data/models/auth_request_models.dart';
import '../../core/di/injection.dart';
import '../../core/router/app_router.dart';
import '../../core/theme/app_theme.dart';
import '../../core/usecases/auth_usecases.dart';

/// Two-step password reset:
///   1. Enter the account email (e.g. a Gmail address) → a 6-digit code is emailed.
///   2. Enter the code and a new password.
class ForgotPasswordScreen extends StatefulWidget {
  final String? initialEmail;

  const ForgotPasswordScreen({super.key, this.initialEmail});

  @override
  State<ForgotPasswordScreen> createState() => _ForgotPasswordScreenState();
}

class _ForgotPasswordScreenState extends State<ForgotPasswordScreen> {
  final _emailFormKey = GlobalKey<FormState>();
  final _resetFormKey = GlobalKey<FormState>();
  late final TextEditingController _emailController;
  final _codeController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmController = TextEditingController();

  bool _codeSent = false;
  bool _isLoading = false;
  bool _obscurePassword = true;

  @override
  void initState() {
    super.initState();
    _emailController = TextEditingController(text: widget.initialEmail ?? '');
  }

  @override
  void dispose() {
    _emailController.dispose();
    _codeController.dispose();
    _passwordController.dispose();
    _confirmController.dispose();
    super.dispose();
  }

  void _toast(String msg, {bool error = false}) {
    Fluttertoast.showToast(
      msg: msg,
      toastLength: Toast.LENGTH_LONG,
      gravity: ToastGravity.BOTTOM,
      backgroundColor: error ? Colors.red.shade700 : AppTheme.successColor,
      textColor: Colors.white,
    );
  }

  Future<void> _sendCode() async {
    // The email form is only mounted on step 1; "Resend" on step 2 reuses the
    // already-validated address.
    final emailForm = _emailFormKey.currentState;
    if (emailForm != null && !emailForm.validate()) return;
    if (_validateEmail(_emailController.text) != null) return;
    setState(() => _isLoading = true);

    final result =
        await sl<ForgotPasswordUseCase>()(_emailController.text.trim());

    if (!mounted) return;
    setState(() => _isLoading = false);

    result.fold(
      (failure) => _toast(failure.message, error: true),
      (_) {
        setState(() => _codeSent = true);
        _toast('If an account exists, a 6-digit code was sent to your email.');
      },
    );
  }

  Future<void> _resetPassword() async {
    if (!_resetFormKey.currentState!.validate()) return;
    setState(() => _isLoading = true);

    final result = await sl<ResetPasswordUseCase>()(ResetPasswordRequest(
      email: _emailController.text.trim(),
      code: _codeController.text.trim(),
      newPassword: _passwordController.text,
      confirmPassword: _confirmController.text,
    ));

    if (!mounted) return;
    setState(() => _isLoading = false);

    result.fold(
      (failure) => _toast(failure.message, error: true),
      (_) {
        _toast('Password reset! Sign in with your new password.');
        context.go(AppRoutes.login);
      },
    );
  }

  String? _validateEmail(String? value) {
    final email = value?.trim() ?? '';
    if (email.isEmpty) return 'Email is required';
    if (!RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$').hasMatch(email)) {
      return 'Enter a valid email';
    }
    return null;
  }

  String? _validatePassword(String? value) {
    final v = value ?? '';
    if (v.length < 8) return 'At least 8 characters';
    if (!v.contains(RegExp(r'[A-Z]'))) return 'Add an uppercase letter';
    if (!v.contains(RegExp(r'[a-z]'))) return 'Add a lowercase letter';
    if (!v.contains(RegExp(r'\d'))) return 'Add a number';
    if (!v.contains(RegExp(r'[^A-Za-z0-9]'))) return 'Add a symbol (e.g. !@#)';
    return null;
  }

  Widget _loadingOr(String label) => _isLoading
      ? const SizedBox(
          width: 22,
          height: 22,
          child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2),
        )
      : Text(label);

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        backgroundColor: Colors.white,
        foregroundColor: AppTheme.textPrimary,
        systemOverlayStyle: SystemUiOverlayStyle.dark,
        title: const Text(''),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_rounded),
          onPressed: () {
            if (_codeSent) {
              setState(() => _codeSent = false);
            } else if (context.canPop()) {
              context.pop();
            } else {
              context.go(AppRoutes.login);
            }
          },
        ),
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.fromLTRB(24, 8, 24, 32),
          child: AnimatedSwitcher(
            duration: const Duration(milliseconds: 250),
            child: _codeSent ? _buildResetStep(theme) : _buildEmailStep(theme),
          ),
        ),
      ),
    );
  }

  Widget _header(ThemeData theme, IconData icon, String title, String subtitle) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Container(
          width: 56,
          height: 56,
          decoration: BoxDecoration(
            color: AppTheme.primarySoft,
            borderRadius: BorderRadius.circular(16),
          ),
          child: Icon(icon, color: AppTheme.primaryColor, size: 28),
        ),
        const SizedBox(height: 20),
        Text(title, style: theme.textTheme.headlineMedium),
        const SizedBox(height: 8),
        Text(subtitle, style: theme.textTheme.bodyMedium),
        const SizedBox(height: 28),
      ],
    );
  }

  Widget _buildEmailStep(ThemeData theme) {
    return Form(
      key: _emailFormKey,
      child: Column(
        key: const ValueKey('email-step'),
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          _header(
            theme,
            Icons.mark_email_unread_outlined,
            'Forgot password?',
            'Enter the email you registered with (e.g. your Gmail) and we\'ll '
                'send you a 6-digit code to reset your password.',
          ),
          TextFormField(
            controller: _emailController,
            keyboardType: TextInputType.emailAddress,
            textInputAction: TextInputAction.done,
            autofillHints: const [AutofillHints.email],
            onFieldSubmitted: (_) => _sendCode(),
            decoration: const InputDecoration(
              labelText: 'Email address',
              hintText: 'you@gmail.com',
              prefixIcon: Icon(Icons.email_outlined),
            ),
            validator: _validateEmail,
          ),
          const SizedBox(height: 24),
          ElevatedButton(
            onPressed: _isLoading ? null : _sendCode,
            child: _loadingOr('Send reset code'),
          ),
          const SizedBox(height: 12),
          TextButton(
            onPressed: () {
              if (_validateEmail(_emailController.text) == null) {
                setState(() => _codeSent = true);
              } else {
                _emailFormKey.currentState!.validate();
              }
            },
            child: const Text('I already have a code'),
          ),
        ],
      ),
    );
  }

  Widget _buildResetStep(ThemeData theme) {
    return Form(
      key: _resetFormKey,
      child: Column(
        key: const ValueKey('reset-step'),
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          _header(
            theme,
            Icons.lock_reset_rounded,
            'Check your email',
            'We sent a 6-digit code to ${_emailController.text.trim()}. '
                'It expires in a few minutes — check spam if you don\'t see it.',
          ),
          TextFormField(
            controller: _codeController,
            keyboardType: TextInputType.number,
            textAlign: TextAlign.center,
            maxLength: 6,
            autofillHints: const [AutofillHints.oneTimeCode],
            inputFormatters: [FilteringTextInputFormatter.digitsOnly],
            style: theme.textTheme.headlineMedium?.copyWith(letterSpacing: 12),
            decoration: const InputDecoration(
              hintText: '••••••',
              counterText: '',
            ),
            validator: (v) =>
                (v ?? '').length == 6 ? null : 'Enter the 6-digit code',
          ),
          const SizedBox(height: 16),
          TextFormField(
            controller: _passwordController,
            obscureText: _obscurePassword,
            textInputAction: TextInputAction.next,
            autofillHints: const [AutofillHints.newPassword],
            decoration: InputDecoration(
              labelText: 'New password',
              prefixIcon: const Icon(Icons.lock_outline),
              helperText: '8+ chars with upper, lower, number & symbol',
              suffixIcon: IconButton(
                icon: Icon(_obscurePassword
                    ? Icons.visibility_off_outlined
                    : Icons.visibility_outlined),
                onPressed: () =>
                    setState(() => _obscurePassword = !_obscurePassword),
              ),
            ),
            validator: _validatePassword,
          ),
          const SizedBox(height: 16),
          TextFormField(
            controller: _confirmController,
            obscureText: _obscurePassword,
            textInputAction: TextInputAction.done,
            onFieldSubmitted: (_) => _resetPassword(),
            decoration: const InputDecoration(
              labelText: 'Confirm new password',
              prefixIcon: Icon(Icons.lock_outline),
            ),
            validator: (v) =>
                v == _passwordController.text ? null : 'Passwords do not match',
          ),
          const SizedBox(height: 24),
          ElevatedButton(
            onPressed: _isLoading ? null : _resetPassword,
            child: _loadingOr('Reset password'),
          ),
          const SizedBox(height: 12),
          TextButton(
            onPressed: _isLoading ? null : _sendCode,
            child: const Text('Resend code'),
          ),
        ],
      ),
    );
  }
}
