import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../../core/constants/app_constants.dart';
import '../../core/data/local/secure_storage_service.dart';
import '../../core/data/models/user_model.dart';
import '../../core/di/injection.dart';
import '../../core/router/app_router.dart';
import '../../core/theme/app_theme.dart';

class RoleSelectionScreen extends StatefulWidget {
  const RoleSelectionScreen({super.key});

  @override
  State<RoleSelectionScreen> createState() => _RoleSelectionScreenState();
}

class _RoleSelectionScreenState extends State<RoleSelectionScreen> {
  AppUser? _user;

  @override
  void initState() {
    super.initState();
    _loadUser();
  }

  Future<void> _loadUser() async {
    final storage = sl<SecureStorageService>();
    final userJson = await storage.read(AppConstants.userKey);
    if (!mounted) return;

    if (userJson != null) {
      try {
        final user = AppUser.fromJson(
            Map<String, dynamic>.from(jsonDecode(userJson) as Map));
        setState(() => _user = user);
      } catch (_) {}
    }
  }

  Future<void> _selectRole(AppRole role) async {
    final storage = sl<SecureStorageService>();
    await storage.write(
      AppConstants.selectedRoleKey,
      role.backendRoleName,
    );

    if (!mounted) return;

    switch (role) {
      case AppRole.customer:
        context.go(AppRoutes.customerHome);
        break;
      case AppRole.pickupDriver:
      case AppRole.deliveryDriver:
        context.go(AppRoutes.driverHome);
        break;
      case AppRole.admin:
        context.go(AppRoutes.adminHome);
        break;
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final availableRoles = _user?.appRoles ?? [];

    return Scaffold(
      backgroundColor: theme.colorScheme.surface,
      appBar: AppBar(
        title: const Text('Select Role'),
        automaticallyImplyLeading: false,
      ),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 32),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const Text(
                'Choose your role',
                textAlign: TextAlign.center,
                style: TextStyle(
                  fontSize: 24,
                  fontWeight: FontWeight.bold,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                'You have multiple roles. Select the one you want to use.',
                textAlign: TextAlign.center,
                style: theme.textTheme.bodyMedium,
              ),
              const SizedBox(height: 32),
              if (availableRoles.isEmpty)
                const Center(child: CircularProgressIndicator())
              else
                ...availableRoles.map((role) => _RoleCard(
                      role: role,
                      onTap: () => _selectRole(role),
                    )),
            ],
          ),
        ),
      ),
    );
  }
}

class _RoleCard extends StatelessWidget {
  final AppRole role;
  final VoidCallback onTap;

  const _RoleCard({
    required this.role,
    required this.onTap,
  });

  IconData get _icon {
    switch (role) {
      case AppRole.customer:
        return Icons.person_outline;
      case AppRole.pickupDriver:
      case AppRole.deliveryDriver:
        return Icons.local_shipping;
      case AppRole.admin:
        return Icons.admin_panel_settings;
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: ListTile(
        leading: CircleAvatar(
          backgroundColor: AppTheme.primaryColor.withValues(alpha: 0.1),
          child: Icon(_icon, color: AppTheme.primaryColor),
        ),
        title: Text(
          role.displayName,
          style: theme.textTheme.labelLarge,
        ),
        trailing: const Icon(Icons.chevron_right),
        onTap: onTap,
      ),
    );
  }
}
