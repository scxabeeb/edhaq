import 'dart:convert';

import 'package:go_router/go_router.dart';
import '../../core/constants/app_constants.dart';
import '../../core/data/local/secure_storage_service.dart';
import '../../core/data/models/user_model.dart';
import '../../core/di/injection.dart';
import '../../features/auth/login_screen.dart';
import '../../features/auth/register_screen.dart';
import '../../features/auth/role_selection_screen.dart';
import '../../features/customer/addresses_screen.dart';
import '../../features/customer/add_address_screen.dart';
import '../../features/customer/create_order_screen.dart';
import '../../features/customer/home_screen.dart';
import '../../features/customer/notifications_screen.dart';
import '../../features/customer/order_detail_screen.dart';
import '../../features/customer/orders_screen.dart';
import '../../features/customer/profile_screen.dart';
import '../../features/driver/assignments_screen.dart';
import '../../features/driver/home_screen.dart';
import '../../features/admin/home_screen.dart';
import '../../features/admin/orders_screen.dart';
import '../../features/admin/services_screen.dart';
import '../../features/admin/users_screen.dart';
import '../../features/splash/splash_screen.dart';

/// Route names for the eDhaq app.
abstract class AppRoutes {
  static const String splash = '/';
  static const String login = '/login';
  static const String register = '/register';
  static const String roleSelection = '/role-selection';
  static const String customerHome = '/home';
  static const String orders = '/orders';
  static const String orderDetail = '/orders/detail';
  static const String createOrder = '/orders/create';
  static const String addresses = '/addresses';
  static const String addAddress = '/addresses/add';
  static const String notifications = '/notifications';
  static const String profile = '/profile';
  static const String driverHome = '/driver';
  static const String driverAssignments = '/driver/assignments';
  static const String adminHome = '/admin';
  static const String adminOrders = '/admin/orders';
  static const String adminDrivers = '/admin/drivers';
  static const String adminUsers = '/admin/users';
  static const String adminServices = '/admin/services';
  static const String adminOrderDetail = '/admin/orders/detail';
}

/// Determines the redirect target for a given role.
String _redirectForRole(String role) {
  switch (role) {
    case 'Customer':
      return AppRoutes.customerHome;
    case 'PickupDriver':
    case 'DeliveryDriver':
      return AppRoutes.driverHome;
    case 'Administrator':
    case 'Manager':
      return AppRoutes.adminHome;
    default:
      return AppRoutes.login;
  }
}

/// Loads the current user from cached storage.
Future<AppUser?> _loadCachedUser() async {
  final storage = sl<SecureStorageService>();
  final userJson = await storage.read(AppConstants.userKey);
  if (userJson == null) return null;
  try {
    return AppUser.fromJson(
        Map<String, dynamic>.from(jsonDecode(userJson) as Map));
  } catch (_) {
    return null;
  }
}

/// Central GoRouter configuration with role-based access control.
final GoRouter appRouter = GoRouter(
  initialLocation: AppRoutes.splash,
  redirect: (context, state) async {
    final storage = sl<SecureStorageService>();
    final token = await storage.read(AppConstants.authTokenKey);
    final isLoggedIn = token != null && token.isNotEmpty;

    final loggingIn = state.matchedLocation == AppRoutes.login ||
        state.matchedLocation == AppRoutes.register;
    final selectingRole = state.matchedLocation == AppRoutes.roleSelection;

    // Not logged in → send to login (unless already on splash/login/register)
    if (!isLoggedIn) {
      if (loggingIn || state.matchedLocation == AppRoutes.splash) return null;
      return AppRoutes.login;
    }

    // Logged in but trying to access login/register → go to splash
    if (loggingIn) {
      return AppRoutes.splash;
    }

    // Splash or role-selection → let the screen handle routing
    if (state.matchedLocation == AppRoutes.splash || selectingRole) {
      return null;
    }

    final user = await _loadCachedUser();
    if (user == null) return null;

    final selectedRoleStr = await storage.read(AppConstants.selectedRoleKey);
    final roles = user.appRoles;

    String? effectiveRole;
    if (selectedRoleStr != null && selectedRoleStr.isNotEmpty) {
      effectiveRole = selectedRoleStr;
    } else if (roles.isNotEmpty) {
      effectiveRole = roles.first.backendRoleName;
    }

    if (effectiveRole == null) return AppRoutes.login;

    final location = state.matchedLocation;

    final isCustomerRoute = location == AppRoutes.customerHome ||
        location == AppRoutes.createOrder ||
        location.startsWith(AppRoutes.addresses) ||
        (location.startsWith(AppRoutes.orders) && !location.startsWith(AppRoutes.adminOrders));

    final isDriverRoute =
        location == AppRoutes.driverHome || location == AppRoutes.driverAssignments;

    final isAdminRoute = location == AppRoutes.adminHome ||
        location.startsWith(AppRoutes.adminOrders);

    // Notifications and profile are accessible to all roles
    final isUniversalRoute =
        location == AppRoutes.notifications || location == AppRoutes.profile;

    if (isUniversalRoute) return null;

    if (isCustomerRoute && !user.isCustomer) {
      return _redirectForRole(effectiveRole);
    }

    if (isDriverRoute && !user.isDriver) {
      return _redirectForRole(effectiveRole);
    }

    if (isAdminRoute && !user.isAdmin) {
      return _redirectForRole(effectiveRole);
    }
    return null;
  },
  routes: [
    GoRoute(
      path: AppRoutes.splash,
      name: 'splash',
      builder: (context, state) => const SplashScreen(),
    ),
    GoRoute(
      path: AppRoutes.login,
      name: 'login',
      builder: (context, state) => const LoginScreen(),
    ),
    GoRoute(
      path: AppRoutes.register,
      name: 'register',
      builder: (context, state) => const RegisterScreen(),
    ),
    GoRoute(
      path: AppRoutes.roleSelection,
      name: 'roleSelection',
      builder: (context, state) => const RoleSelectionScreen(),
    ),
    GoRoute(
      path: AppRoutes.customerHome,
      name: 'customerHome',
      builder: (context, state) => const HomeScreen(),
    ),
    GoRoute(
      path: AppRoutes.orders,
      name: 'orders',
      builder: (context, state) => const OrdersScreen(),
    ),
    GoRoute(
      path: '${AppRoutes.orderDetail}/:id',
      name: 'orderDetail',
      builder: (context, state) {
        final id = int.tryParse(state.pathParameters['id'] ?? '0') ?? 0;
        return OrderDetailScreen(orderId: id);
      },
    ),
    GoRoute(
      path: AppRoutes.createOrder,
      name: 'createOrder',
      builder: (context, state) => const CreateOrderScreen(),
    ),
    GoRoute(
      path: AppRoutes.addresses,
      name: 'addresses',
      builder: (context, state) => const AddressesScreen(),
    ),
    GoRoute(
      path: AppRoutes.addAddress,
      name: 'addAddress',
      builder: (context, state) => const AddAddressScreen(),
    ),
    GoRoute(
      path: AppRoutes.notifications,
      name: 'notifications',
      builder: (context, state) => const NotificationsScreen(),
    ),
    GoRoute(
      path: AppRoutes.profile,
      name: 'profile',
      builder: (context, state) => const ProfileScreen(),
    ),
    GoRoute(
      path: AppRoutes.driverHome,
      name: 'driverHome',
      builder: (context, state) => const DriverHomeScreen(),
    ),
    GoRoute(
      path: AppRoutes.driverAssignments,
      name: 'driverAssignments',
      builder: (context, state) => const DriverAssignmentsScreen(),
    ),
    GoRoute(
      path: AppRoutes.adminHome,
      name: 'adminHome',
      builder: (context, state) => const AdminHomeScreen(),
    ),
    GoRoute(
      path: AppRoutes.adminOrders,
      name: 'adminOrders',
      builder: (context, state) => AdminOrdersScreen(
        initialFilter: state.extra is AdminOrdersFilter
            ? state.extra as AdminOrdersFilter
            : null,
      ),
    ),
    GoRoute(
      path: AppRoutes.adminDrivers,
      name: 'adminDrivers',
      builder: (context, state) => const AdminUsersScreen(
        title: 'Drivers',
        role: 'Driver',
      ),
    ),
    GoRoute(
      path: AppRoutes.adminUsers,
      name: 'adminUsers',
      builder: (context, state) =>
          const AdminUsersScreen(title: 'Users'),
    ),
    GoRoute(
      path: AppRoutes.adminServices,
      name: 'adminServices',
      builder: (context, state) => const AdminServicesScreen(),
    ),
    GoRoute(
      path: '${AppRoutes.adminOrderDetail}/:id',
      name: 'adminOrderDetail',
      builder: (context, state) {
        final id = int.tryParse(state.pathParameters['id'] ?? '0') ?? 0;
        return OrderDetailScreen(orderId: id);
      },
    ),
  ],
);
