import 'package:equatable/equatable.dart';

class AddressModel extends Equatable {
  final int id;
  final String label;
  final String street;
  final String? district;
  final int cityId;
  final String? cityName;
  final int villageId;
  final String? villageName;
  final int? subVillageId;
  final String? subVillageName;
  final double? latitude;
  final double? longitude;
  final bool isDefault;

  const AddressModel({
    required this.id,
    required this.label,
    required this.street,
    this.district,
    required this.cityId,
    this.cityName,
    required this.villageId,
    this.villageName,
    this.subVillageId,
    this.subVillageName,
    this.latitude,
    this.longitude,
    this.isDefault = false,
  });

  String get fullAddress {
    final parts = <String>[
      street,
      if (district != null && district!.isNotEmpty) district!,
      if (subVillageName != null && subVillageName!.isNotEmpty) subVillageName!,
      if (villageName != null && villageName!.isNotEmpty) villageName!,
      if (cityName != null && cityName!.isNotEmpty) cityName!,
    ];
    return parts.join(', ');
  }

  factory AddressModel.fromJson(Map<String, dynamic> json) => AddressModel(
        id: json['id'] as int? ?? 0,
        label: json['label'] as String? ?? '',
        street: json['street'] as String? ?? '',
        district: json['district'] as String?,
        cityId: json['cityId'] as int? ?? 0,
        cityName: json['cityName'] as String?,
        villageId: json['villageId'] as int? ?? 0,
        villageName: json['villageName'] as String?,
        subVillageId: json['subVillageId'] as int?,
        subVillageName: json['subVillageName'] as String?,
        latitude: _toDoubleOrNull(json['latitude']),
        longitude: _toDoubleOrNull(json['longitude']),
        isDefault: json['isDefault'] as bool? ?? false,
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'label': label,
        'street': street,
        'district': district,
        'cityId': cityId,
        'cityName': cityName,
        'villageId': villageId,
        'villageName': villageName,
        'subVillageId': subVillageId,
        'subVillageName': subVillageName,
        'latitude': latitude,
        'longitude': longitude,
        'isDefault': isDefault,
      };

  static double? _toDoubleOrNull(dynamic value) {
    if (value == null) return null;
    if (value is double) return value;
    if (value is int) return value.toDouble();
    if (value is String) return double.tryParse(value);
    return null;
  }

  @override
  List<Object?> get props => [
        id,
        label,
        street,
        district,
        cityId,
        cityName,
        villageId,
        villageName,
        subVillageId,
        subVillageName,
        latitude,
        longitude,
        isDefault,
      ];
}

class CreateAddressRequest {
  final String label;
  final String street;
  final String? district;
  final int cityId;
  final int villageId;
  final int? subVillageId;
  final double? latitude;
  final double? longitude;
  final bool isDefault;

  const CreateAddressRequest({
    this.label = 'Home',
    required this.street,
    this.district,
    required this.cityId,
    required this.villageId,
    this.subVillageId,
    this.latitude,
    this.longitude,
    this.isDefault = false,
  });

  Map<String, dynamic> toJson() => {
        'label': label,
        'street': street,
        'district': district,
        'cityId': cityId,
        'villageId': villageId,
        'subVillageId': subVillageId,
        'latitude': latitude,
        'longitude': longitude,
        'isDefault': isDefault,
      };
}