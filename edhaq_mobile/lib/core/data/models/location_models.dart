import 'package:equatable/equatable.dart';

class CityModel extends Equatable {
  final int id;
  final String name;
  final String? country;
  final bool isActive;

  const CityModel({
    required this.id,
    required this.name,
    this.country,
    this.isActive = true,
  });

  factory CityModel.fromJson(Map<String, dynamic> json) => CityModel(
        id: json['id'] as int? ?? 0,
        name: json['name'] as String? ?? '',
        country: json['country'] as String?,
        isActive: json['isActive'] as bool? ?? true,
      );

  Map<String, dynamic> toJson() =>
      {'id': id, 'name': name, 'country': country, 'isActive': isActive};

  @override
  List<Object?> get props => [id, name, country, isActive];
}

class VillageModel extends Equatable {
  final int id;
  final int cityId;
  final String name;
  final bool isActive;

  const VillageModel({
    required this.id,
    required this.cityId,
    required this.name,
    this.isActive = true,
  });

  factory VillageModel.fromJson(Map<String, dynamic> json) => VillageModel(
        id: json['id'] as int? ?? 0,
        cityId: json['cityId'] as int? ?? 0,
        name: json['name'] as String? ?? '',
        isActive: json['isActive'] as bool? ?? true,
      );

  Map<String, dynamic> toJson() =>
      {'id': id, 'cityId': cityId, 'name': name, 'isActive': isActive};

  @override
  List<Object?> get props => [id, cityId, name, isActive];
}

class SubVillageModel extends Equatable {
  final int id;
  final int villageId;
  final String name;
  final bool isActive;

  const SubVillageModel({
    required this.id,
    required this.villageId,
    required this.name,
    this.isActive = true,
  });

  factory SubVillageModel.fromJson(Map<String, dynamic> json) => SubVillageModel(
        id: json['id'] as int? ?? 0,
        villageId: json['villageId'] as int? ?? 0,
        name: json['name'] as String? ?? '',
        isActive: json['isActive'] as bool? ?? true,
      );

  Map<String, dynamic> toJson() =>
      {'id': id, 'villageId': villageId, 'name': name, 'isActive': isActive};

  @override
  List<Object?> get props => [id, villageId, name, isActive];
}