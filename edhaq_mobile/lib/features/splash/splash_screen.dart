import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../../core/constants/app_constants.dart';
import '../../core/data/local/secure_storage_service.dart';
import '../../core/data/models/user_model.dart';
import '../../core/di/injection.dart';
import '../../core/router/app_router.dart';
import '../../core/usecases/auth_usecases.dart';
import '../../core/usecases/usecase.dart';
import '../../core/widgets/app_logo.dart';

class SplashScreen extends StatefulWidget {
  const SplashScreen({super.key});

  @override
  State<SplashScreen> createState() => _SplashScreenState();
}

class _SplashScreenState extends State<SplashScreen> {
  @override
  void initState() {
    super.initState();
    _checkAuth();
  }

  Future<void> _checkAuth() async {
    await Future.delayed(const Duration(seconds: 2));

    if (!mounted) return;

    final storage = sl<SecureStorageService>();
    final token = await storage.read(AppConstants.authTokenKey);

    if (!mounted) return;

    if (token == null || token.isEmpty) {
      context.go(AppRoutes.login);
      return;
    }

    AppUser? user;
    final userJson = await storage.read(AppConstants.userKey);
    if (userJson != null) {
      try {
        user = AppUser.fromJson(
            Map<String, dynamic>.from(jsonDecode(userJson) as Map));
      } catch (_) {
        user = null;
      }
    }

    // Always revalidate the token against the API. A cached user alone is
    // not enough — the token may reference a user that no longer exists
    // (re-seeded DB) or may have been revoked. The DioClient interceptor
    // clears storage on 401 / 404-of-/me, so a failure here means login.
    if (token.isNotEmpty) {
      final result = await sl<GetCurrentUserUseCase>()(const NoParams());
      if (!mounted) return;

      var validated = false;
      await result.fold(
        (failure) async {
          await storage.deleteAll();
          validated = false;
        },
        (fetchedUser) async {
          user = fetchedUser;
          await storage.write(
            AppConstants.userKey,
            jsonEncode(fetchedUser.toJson()),
          );
          validated = true;
        },
      );

      if (!validated) {
        if (!mounted) return;
        context.go(AppRoutes.login);
        return;
      }

      if (!mounted) return;
      await _routeUser(user!, storage);
      return;
    }

    if (!mounted) return;
    context.go(AppRoutes.login);
  }

  Future<void> _routeUser(AppUser user, SecureStorageService storage) async {
    if (!mounted) return;

    final selectedRoleStr = await storage.read(AppConstants.selectedRoleKey);
    if (!mounted) return;

    final roles = user.appRoles;

    if (roles.length > 1) {
      if (selectedRoleStr != null && selectedRoleStr.isNotEmpty) {
        _navigateByRole(selectedRoleStr);
      } else {
        context.go(AppRoutes.roleSelection);
      }
    } else if (roles.isNotEmpty) {
      _navigateByRole(roles.first.backendRoleName);
    } else {
      context.go(AppRoutes.login);
    }
  }

  void _navigateByRole(String role) {
    switch (role) {
      case 'Customer':
        context.go(AppRoutes.customerHome);
        break;
      case 'PickupDriver':
      case 'DeliveryDriver':
        context.go(AppRoutes.driverHome);
        break;
      case 'Administrator':
      case 'Manager':
        context.go(AppRoutes.adminHome);
        break;
      default:
        context.go(AppRoutes.login);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      backgroundColor: theme.colorScheme.primary,
      body: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            AppLogo(
              size: 96,
              backgroundColor: Colors.white,
              showBrandBorder: true,
              borderRadius: 24,
            ),
            const SizedBox(height: 24),
            const Text(
              'eDhaq',
              style: TextStyle(
                color: Colors.white,
                fontSize: 40,
                fontWeight: FontWeight.bold,
                letterSpacing: 2,
              ),
            ),
            const SizedBox(height: 8),
            const Text(
              'Laundry, delivered.',
              style: TextStyle(
                color: Colors.white70,
                fontSize: 16,
              ),
            ),
            const SizedBox(height: 48),
            const SizedBox(
              width: 32,
              height: 32,
              child: CircularProgressIndicator(
                color: Colors.white,
                strokeWidth: 3,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
