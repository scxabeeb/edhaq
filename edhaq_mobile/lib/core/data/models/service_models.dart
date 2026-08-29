import 'package:equatable/equatable.dart';

class ServiceCategoryModel extends Equatable {
  final int id;
  final String name;
  final String? description;
  final String? iconClass;
  final int sortOrder;
  final bool isActive;

  const ServiceCategoryModel({
    required this.id,
    required this.name,
    this.description,
    this.iconClass,
    this.sortOrder = 0,
    this.isActive = true,
  });

  factory ServiceCategoryModel.fromJson(Map<String, dynamic> json) =>
      ServiceCategoryModel(
        id: json['id'] as int? ?? 0,
        name: json['name'] as String? ?? '',
        description: json['description'] as String?,
        iconClass: json['iconClass'] as String?,
        sortOrder: json['sortOrder'] as int? ?? 0,
        isActive: json['isActive'] as bool? ?? true,
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'name': name,
        'description': description,
        'iconClass': iconClass,
        'sortOrder': sortOrder,
        'isActive': isActive,
      };

  @override
  List<Object?> get props => [id, name, description, iconClass, sortOrder, isActive];
}

class ServiceModel extends Equatable {
  final int id;
  final String name;
  final String? description;
  final int categoryId;
  final String? categoryName;
  final double pricePerPiece;
  final double? pricePerKg;
  final int estimatedHours;
  final bool isExpress;
  final bool isActive;
  final String? iconClass;
  final int sortOrder;

  const ServiceModel({
    required this.id,
    required this.name,
    this.description,
    required this.categoryId,
    this.categoryName,
    this.pricePerPiece = 0,
    this.pricePerKg,
    this.estimatedHours = 0,
    this.isExpress = false,
    this.isActive = true,
    this.iconClass,
    this.sortOrder = 0,
  });

  factory ServiceModel.fromJson(Map<String, dynamic> json) => ServiceModel(
        id: json['id'] as int? ?? 0,
        name: json['name'] as String? ?? '',
        description: json['description'] as String?,
        categoryId: json['categoryId'] as int? ?? 0,
        categoryName: json['categoryName'] as String?,
        pricePerPiece: _toDouble(json['pricePerPiece']),
        pricePerKg: _toDoubleOrNull(json['pricePerKg']),
        estimatedHours: json['estimatedHours'] as int? ?? 0,
        isExpress: json['isExpress'] as bool? ?? false,
        isActive: json['isActive'] as bool? ?? true,
        iconClass: json['iconClass'] as String?,
        sortOrder: json['sortOrder'] as int? ?? 0,
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'name': name,
        'description': description,
        'categoryId': categoryId,
        'categoryName': categoryName,
        'pricePerPiece': pricePerPiece,
        'pricePerKg': pricePerKg,
        'estimatedHours': estimatedHours,
        'isExpress': isExpress,
        'isActive': isActive,
        'iconClass': iconClass,
        'sortOrder': sortOrder,
      };

  static double _toDouble(dynamic value) {
    if (value == null) return 0;
    if (value is double) return value;
    if (value is int) return value.toDouble();
    if (value is String) return double.tryParse(value) ?? 0;
    return 0;
  }

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
        name,
        description,
        categoryId,
        categoryName,
        pricePerPiece,
        pricePerKg,
        estimatedHours,
        isExpress,
        isActive,
        iconClass,
        sortOrder,
      ];
}