import 'package:equatable/equatable.dart';

import '../../constants/app_constants.dart';

class AppUser extends Equatable {
  final String id;
  final String email;
  final String firstName;
  final String lastName;
  final String? phoneNumber;
  final String? profileImageUrl;
  final List<String> roles;
  final int? customerId;
  final double walletBalance;

  const AppUser({
    required this.id,
    required this.email,
    required this.firstName,
    required this.lastName,
    this.phoneNumber,
    this.profileImageUrl,
    this.roles = const [],
    this.customerId,
    this.walletBalance = 0,
  });

  String get fullName => '$firstName $lastName'.trim();
  bool get isCustomer => roles.contains('Customer');
  bool get isDriver => roles.contains('PickupDriver') || roles.contains('DeliveryDriver');
  bool get isAdmin => roles.contains('Administrator') || roles.contains('Manager');
  bool get isPickupDriver => roles.contains('PickupDriver');
  bool get isDeliveryDriver => roles.contains('DeliveryDriver');

  List<AppRole> get appRoles {
    final result = <AppRole>[];
    if (isAdmin) result.add(AppRole.admin);
    if (isPickupDriver) result.add(AppRole.pickupDriver);
    if (isDeliveryDriver) result.add(AppRole.deliveryDriver);
    if (isCustomer) result.add(AppRole.customer);
    return result;
  }

  bool get hasMultipleRoles => appRoles.length > 1;

  AppUser copyWith({
    String? id,
    String? email,
    String? firstName,
    String? lastName,
    String? phoneNumber,
    String? profileImageUrl,
    List<String>? roles,
    int? customerId,
    double? walletBalance,
  }) {
    return AppUser(
      id: id ?? this.id,
      email: email ?? this.email,
      firstName: firstName ?? this.firstName,
      lastName: lastName ?? this.lastName,
      phoneNumber: phoneNumber ?? this.phoneNumber,
      profileImageUrl: profileImageUrl ?? this.profileImageUrl,
      roles: roles ?? this.roles,
      customerId: customerId ?? this.customerId,
      walletBalance: walletBalance ?? this.walletBalance,
    );
  }

  factory AppUser.fromJson(Map<String, dynamic> json) => AppUser(
        id: json['id'] as String? ?? json['userId'] as String? ?? '',
        email: json['email'] as String? ?? '',
        firstName: json['firstName'] as String? ?? '',
        lastName: json['lastName'] as String? ?? '',
        phoneNumber: json['phoneNumber'] as String?,
        profileImageUrl: json['profileImageUrl'] as String?,
        roles: (json['roles'] as List<dynamic>?)?.cast<String>() ?? [],
        customerId: json['customerId'] as int?,
        walletBalance: _toDouble(json['walletBalance']),
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'email': email,
        'firstName': firstName,
        'lastName': lastName,
        'phoneNumber': phoneNumber,
        'profileImageUrl': profileImageUrl,
        'roles': roles,
        'customerId': customerId,
        'walletBalance': walletBalance,
      };

  static double _toDouble(dynamic value) {
    if (value == null) return 0;
    if (value is double) return value;
    if (value is int) return value.toDouble();
    if (value is String) return double.tryParse(value) ?? 0;
    return 0;
  }

  @override
  List<Object?> get props => [
        id, email, firstName, lastName, phoneNumber, profileImageUrl, roles, customerId, walletBalance
      ];
}

class LoginResponse {
  final String token;
  final DateTime? expiresOn;
  final AppUser user;

  LoginResponse({required this.token, this.expiresOn, required this.user});

  static DateTime? _parseDate(dynamic value) {
    if (value == null) return null;
    if (value is DateTime) return value;
    return DateTime.tryParse(value.toString());
  }

  factory LoginResponse.fromJson(Map<String, dynamic> json) => LoginResponse(
        token: json['token'] as String? ?? '',
        expiresOn: _parseDate(json['expiresOn']) ?? _parseDate(json['expiresOnUtc']),
        user: AppUser.fromJson(Map<String, dynamic>.from(json['user'] as Map)),
      );

  Map<String, dynamic> toJson() => {
        'token': token,
        'expiresOn': expiresOn?.toIso8601String(),
        'user': user.toJson(),
      };
}
