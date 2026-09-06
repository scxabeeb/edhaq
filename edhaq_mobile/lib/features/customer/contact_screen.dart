import 'package:flutter/material.dart';
import 'package:fluttertoast/fluttertoast.dart';
import 'package:go_router/go_router.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../core/data/models/support_contacts_model.dart';
import '../../core/di/injection.dart';
import '../../core/network/api_service.dart';
import '../../core/theme/app_theme.dart';

/// Screen where a customer can contact the eDhaq staff directly
/// via phone call, WhatsApp, or email. Contact details are loaded
/// from the backend (`GET /api/contacts`), falling back to the
/// hard-coded defaults if the request fails.
class ContactScreen extends StatefulWidget {
  const ContactScreen({super.key});

  @override
  State<ContactScreen> createState() => _ContactScreenState();
}

class _ContactScreenState extends State<ContactScreen> {
  SupportContactsModel _contacts = SupportContactsModel.fallback();
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadContacts();
  }

  Future<void> _loadContacts() async {
    setState(() => _isLoading = true);

    try {
      final contacts = await sl<ApiService>().getContacts();
      if (!mounted) return;
      setState(() {
        _contacts = contacts;
        _isLoading = false;
      });
    } catch (_) {
      // Backend unreachable → keep the hard-coded fallback values.
      if (!mounted) return;
      setState(() => _isLoading = false);
    }
  }

  Future<void> _launch(Uri uri) async {
    try {
      final launched =
          await launchUrl(uri, mode: LaunchMode.externalApplication);
      if (!launched) _showError('Could not open the app for this action.');
    } catch (_) {
      _showError('Could not open the app for this action.');
    }
  }

  void _showError(String message) {
    Fluttertoast.showToast(
      msg: message,
      toastLength: Toast.LENGTH_LONG,
      gravity: ToastGravity.BOTTOM,
      backgroundColor: Colors.red.shade700,
      textColor: Colors.white,
    );
  }

  Future<void> _confirmCall() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Call Support'),
        content: Text(
          'Do you want to call our support team at ${_contacts.phone}?',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Cancel'),
          ),
          TextButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Call'),
          ),
        ],
      ),
    );

    if (confirmed == true && mounted) {
      await _launch(Uri(scheme: 'tel', path: _contacts.phone));
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      backgroundColor: theme.colorScheme.surface,
      appBar: AppBar(
        title: const Text('Contact Us'),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.pop(),
        ),
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : RefreshIndicator(
              onRefresh: _loadContacts,
              child: ListView(
                physics: const AlwaysScrollableScrollPhysics(),
                padding: const EdgeInsets.all(16),
                children: [
                  _buildHeader(theme),
                  const SizedBox(height: 24),
                  _ContactTile(
                    icon: Icons.phone_outlined,
                    iconColor: Colors.green.shade600,
                    title: 'Call Support',
                    subtitle: _contacts.phone,
                    onTap: _confirmCall,
                  ),
                  const SizedBox(height: 12),
                  _ContactTile(
                    icon: Icons.chat_outlined,
                    iconColor: Colors.teal.shade600,
                    title: 'WhatsApp',
                    subtitle: _contacts.whatsapp,
                    onTap: () {
                      final number = _contacts.whatsapp.replaceAll('+', '');
                      _launch(Uri.parse('https://wa.me/$number'));
                    },
                  ),
                  const SizedBox(height: 12),
                  _ContactTile(
                    icon: Icons.email_outlined,
                    iconColor: Colors.orange.shade700,
                    title: 'Email Us',
                    subtitle: _contacts.email,
                    onTap: () => _launch(
                      Uri(
                        scheme: 'mailto',
                        path: _contacts.email,
                        query: 'subject=eDhaq Support Request',
                      ),
                    ),
                  ),
                ],
              ),
            ),
    );
  }

  Widget _buildHeader(ThemeData theme) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          children: [
            CircleAvatar(
              radius: 32,
              backgroundColor: AppTheme.primaryColor.withValues(alpha: 0.1),
              child: const Icon(
                Icons.support_agent,
                size: 36,
                color: AppTheme.primaryColor,
              ),
            ),
            const SizedBox(height: 12),
            Text('We are here to help!', style: theme.textTheme.titleMedium),
            const SizedBox(height: 4),
            Text(
              'Reach out to our team any time during working hours.',
              style: theme.textTheme.bodySmall
                  ?.copyWith(color: AppTheme.textSecondary),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 12),
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                const Icon(Icons.schedule,
                    size: 16, color: AppTheme.textSecondary),
                const SizedBox(width: 4),
                Text(
                  _contacts.workingHours,
                  style: theme.textTheme.bodySmall
                      ?.copyWith(color: AppTheme.textSecondary),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _ContactTile extends StatelessWidget {
  final IconData icon;
  final Color iconColor;
  final String title;
  final String subtitle;
  final VoidCallback onTap;

  const _ContactTile({
    required this.icon,
    required this.iconColor,
    required this.title,
    required this.subtitle,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Card(
      child: ListTile(
        leading: CircleAvatar(
          backgroundColor: iconColor.withValues(alpha: 0.1),
          child: Icon(icon, color: iconColor),
        ),
        title: Text(title, style: theme.textTheme.labelLarge),
        subtitle: Text(
          subtitle,
          style: theme.textTheme.bodySmall
              ?.copyWith(color: AppTheme.textSecondary),
        ),
        trailing: const Icon(Icons.chevron_right),
        onTap: onTap,
      ),
    );
  }
}

