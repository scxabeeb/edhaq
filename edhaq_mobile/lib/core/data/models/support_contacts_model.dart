import '../../../core/constants/app_constants.dart';

/// Support contacts fetched from the backend (`GET /api/contacts`).
/// Falls back to the hard-coded defaults in [AppConstants] when the
/// backend is unreachable or a field is missing.
class SupportContactsModel {
  final String phone;
  final String whatsapp;
  final String email;
  final String workingHours;

  const SupportContactsModel({
    required this.phone,
    required this.whatsapp,
    required this.email,
    required this.workingHours,
  });

  factory SupportContactsModel.fallback() => const SupportContactsModel(
        phone: AppConstants.supportPhone,
        whatsapp: AppConstants.supportWhatsapp,
        email: AppConstants.supportEmail,
        workingHours: AppConstants.supportWorkingHours,
      );

  factory SupportContactsModel.fromJson(Map<String, dynamic> json) =>
      SupportContactsModel(
        phone: _nonEmpty(json['phone']) ?? AppConstants.supportPhone,
        whatsapp: _nonEmpty(json['whatsapp']) ?? AppConstants.supportWhatsapp,
        email: _nonEmpty(json['email']) ?? AppConstants.supportEmail,
        workingHours:
            _nonEmpty(json['workingHours']) ?? AppConstants.supportWorkingHours,
      );

  static String? _nonEmpty(Object? value) {
    if (value is String && value.trim().isNotEmpty) return value.trim();
    return null;
  }
}
