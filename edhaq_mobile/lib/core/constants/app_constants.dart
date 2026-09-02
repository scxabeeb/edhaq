class AppConstants {
  AppConstants._();

  static const String appName = 'eDhaq';
  static const String appTagline = 'Laundry, delivered.';

  // Secure storage keys
  static const String authTokenKey = 'auth_token';
  static const String refreshTokenKey = 'refresh_token';
  static const String userKey = 'user';
  static const String selectedRoleKey = 'selected_role';

  // API configuration
  // Production: deployed on Railway (HTTPS)
  // For local dev, uncomment the appropriate line below:
  // static const String baseUrl = 'http://localhost:5058';     // iOS simulator / web / physical device
  // static const String baseUrl = 'http://10.0.2.2:5058';       // Android emulator
  static const String baseUrl = 'https://edhaq-production.up.railway.app';

  static const Duration connectTimeout = Duration(seconds: 30);
  static const Duration receiveTimeout = Duration(seconds: 30);

  // API route prefixes
  static const String apiPrefix = '/api';
  static const String authPath = '/api/auth';
  static const String locationsPath = '/api/locations';
  static const String servicesPath = '/api/services';
  static const String dashboardPath = '/api/dashboard';
  static const String addressesPath = '/api/addresses';
  static const String ordersPath = '/api/orders';
  static const String notificationsPath = '/api/notifications';
  static const String usersPath = '/api/users';
}

enum AppRole {
  customer,
  pickupDriver,
  deliveryDriver,
  admin,
}

extension AppRoleExtension on AppRole {
  String get backendRoleName {
    switch (this) {
      case AppRole.customer:
        return 'Customer';
      case AppRole.pickupDriver:
        return 'PickupDriver';
      case AppRole.deliveryDriver:
        return 'DeliveryDriver';
      case AppRole.admin:
        return 'Administrator';
    }
  }

  bool get isDriver =>
      this == AppRole.pickupDriver || this == AppRole.deliveryDriver;

  bool get isAdmin => this == AppRole.admin;

  String get displayName {
    switch (this) {
      case AppRole.customer:
        return 'Customer';
      case AppRole.pickupDriver:
        return 'Pickup Driver';
      case AppRole.deliveryDriver:
        return 'Delivery Driver';
      case AppRole.admin:
        return 'Admin';
    }
  }
}
