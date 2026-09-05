/// Request payload for the `/api/auth/login` endpoint.
class LoginRequest {
  final String email;
  final String password;
  final bool rememberMe;

  LoginRequest({
    required this.email,
    required this.password,
    this.rememberMe = false,
  });

  Map<String, dynamic> toJson() => {
        'email': email,
        'password': password,
        'rememberMe': rememberMe,
      };
}

/// Request payload for the `/api/auth/change-password` endpoint.
class ChangePasswordRequest {
  final String currentPassword;
  final String newPassword;
  final String confirmPassword;

  ChangePasswordRequest({
    required this.currentPassword,
    required this.newPassword,
    required this.confirmPassword,
  });

  Map<String, dynamic> toJson() => {
        'currentPassword': currentPassword,
        'newPassword': newPassword,
        'confirmPassword': confirmPassword,
      };
}

/// Request payload for the `/api/auth/register` endpoint.
class RegisterRequest {
  final String firstName;
  final String lastName;
  final String email;
  final String phoneNumber;
  final String password;
  final String confirmPassword;
  final int cityId;
  final int villageId;
  final int? subVillageId;
  final String street;
  final String? district;

  RegisterRequest({
    required this.firstName,
    required this.lastName,
    required this.email,
    required this.phoneNumber,
    required this.password,
    required this.confirmPassword,
    required this.cityId,
    required this.villageId,
    this.subVillageId,
    required this.street,
    this.district,
  });

  Map<String, dynamic> toJson() => {
        'firstName': firstName,
        'lastName': lastName,
        'email': email,
        'phoneNumber': phoneNumber,
        'password': password,
        'confirmPassword': confirmPassword,
        'cityId': cityId,
        'villageId': villageId,
        'subVillageId': subVillageId,
        'street': street,
        'district': district,
      };
}
