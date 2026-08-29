import 'package:equatable/equatable.dart';
import 'notification_model.dart';
import 'order_models.dart';

/// Driver's availability status — mirrors backend [DriverStatus] enum.
enum DriverStatus {
  available,
  onDuty,
  offline,
  unknown,
}

extension DriverStatusExtension on DriverStatus {
  String get displayName {
    switch (this) {
      case DriverStatus.available:
        return 'Available';
      case DriverStatus.onDuty:
        return 'On Duty';
      case DriverStatus.offline:
        return 'Offline';
      case DriverStatus.unknown:
        return 'Unknown';
    }
  }

  static DriverStatus fromString(String? value) {
    if (value == null || value.isEmpty) return DriverStatus.unknown;
    return switch (value) {
      'Available' => DriverStatus.available,
      'OnDuty' => DriverStatus.onDuty,
      'Offline' => DriverStatus.offline,
      _ => DriverStatus.unknown,
    };
  }
}

class CustomerDashboardModel extends Equatable {
  final String customerName;
  final int activeOrders;
  final int completedOrders;
  final double walletBalance;
  final List<OrderSummaryModel> recentOrders;
  final List<NotificationModel> unreadNotifications;

  const CustomerDashboardModel({
    required this.customerName,
    required this.activeOrders,
    required this.completedOrders,
    required this.walletBalance,
    this.recentOrders = const [],
    this.unreadNotifications = const [],
  });

  factory CustomerDashboardModel.fromJson(Map<String, dynamic> json) =>
      CustomerDashboardModel(
        customerName: json['customerName'] as String? ?? '',
        activeOrders: json['activeOrders'] as int? ?? 0,
        completedOrders: json['completedOrders'] as int? ?? 0,
        walletBalance: _toDouble(json['walletBalance']),
        recentOrders: (json['recentOrders'] as List<dynamic>?)
                ?.map((e) =>
                    OrderSummaryModel.fromJson(Map<String, dynamic>.from(e)))
                .toList() ??
            [],
        unreadNotifications:
            (json['unreadNotifications'] as List<dynamic>?)
                ?.map((e) => NotificationModel.fromJson(
                    Map<String, dynamic>.from(e)))
                .toList() ??
            [],
      );

  @override
  List<Object?> get props => [
        customerName,
        activeOrders,
        completedOrders,
        walletBalance,
        recentOrders,
        unreadNotifications,
      ];
}

class DriverDashboardModel extends Equatable {
  final String driverName;
  final int activeAssignments;
  final int activePickupAssignments;
  final int activeDeliveryAssignments;
  final double todayEarnings;
  final double totalEarnings;
  final double rating;
  final bool isAvailable;
  final DriverStatus status;
  final List<OrderSummaryModel> currentTasks;
  final List<NotificationModel> unreadNotifications;

  const DriverDashboardModel({
    required this.driverName,
    required this.activeAssignments,
    required this.activePickupAssignments,
    required this.activeDeliveryAssignments,
    required this.todayEarnings,
    required this.totalEarnings,
    required this.rating,
    required this.isAvailable,
    required this.status,
    this.currentTasks = const [],
    this.unreadNotifications = const [],
  });

  factory DriverDashboardModel.fromJson(Map<String, dynamic> json) =>
      DriverDashboardModel(
        driverName: json['driverName'] as String? ?? '',
        activeAssignments: json['activeAssignments'] as int? ?? 0,
        activePickupAssignments: json['activePickupAssignments'] as int? ?? 0,
        activeDeliveryAssignments:
            json['activeDeliveryAssignments'] as int? ?? 0,
        todayEarnings: _toDouble(json['todayEarnings']),
        totalEarnings: _toDouble(json['totalEarnings']),
        rating: _toDouble(json['rating']),
        isAvailable: json['isAvailable'] as bool? ?? false,
        status: DriverStatusExtension.fromString(json['status'] as String?),
        currentTasks: (json['currentTasks'] as List<dynamic>?)
                ?.map((e) =>
                    OrderSummaryModel.fromJson(Map<String, dynamic>.from(e)))
                .toList() ??
            [],
        unreadNotifications:
            (json['unreadNotifications'] as List<dynamic>?)
                ?.map((e) => NotificationModel.fromJson(
                    Map<String, dynamic>.from(e)))
                .toList() ??
            [],
      );

  @override
  List<Object?> get props => [
        driverName,
        activeAssignments,
        activePickupAssignments,
        activeDeliveryAssignments,
        todayEarnings,
        totalEarnings,
        rating,
        isAvailable,
        status,
        currentTasks,
        unreadNotifications,
      ];
}

class AdminDashboardModel extends Equatable {
  final String adminName;
  final int totalOrders;
  final int activeOrders;
  final int completedOrders;
  final double totalRevenue;
  final int totalCustomers;
  final int totalDrivers;
  final Map<String, int> statusCounts;
  final List<OrderSummaryModel> recentOrders;

  const AdminDashboardModel({
    required this.adminName,
    required this.totalOrders,
    required this.activeOrders,
    required this.completedOrders,
    required this.totalRevenue,
    required this.totalCustomers,
    required this.totalDrivers,
    this.statusCounts = const {},
    this.recentOrders = const [],
  });

  factory AdminDashboardModel.fromJson(Map<String, dynamic> json) =>
      AdminDashboardModel(
        adminName: json['adminName'] as String? ?? '',
        totalOrders: json['totalOrders'] as int? ?? 0,
        activeOrders: json['activeOrders'] as int? ?? 0,
        completedOrders: json['completedOrders'] as int? ?? 0,
        totalRevenue: _toDouble(json['totalRevenue']),
        totalCustomers: json['totalCustomers'] as int? ?? 0,
        totalDrivers: json['totalDrivers'] as int? ?? 0,
        statusCounts: (json['statusCounts'] as Map<String, dynamic>?)
                ?.map((key, value) =>
                    MapEntry(key, value as int? ?? 0)) ??
            {},
        recentOrders: (json['recentOrders'] as List<dynamic>?)
                ?.map((e) =>
                    OrderSummaryModel.fromJson(Map<String, dynamic>.from(e)))
                .toList() ??
            [],
      );

  @override
  List<Object?> get props => [
        adminName,
        totalOrders,
        activeOrders,
        completedOrders,
        totalRevenue,
        totalCustomers,
        totalDrivers,
        statusCounts,
        recentOrders,
      ];
}

class DriverAssignmentDetailModel extends Equatable {
  final int id;
  final int orderId;
  final String orderNumber;
  final OrderStatus status;
  final PaymentStatus paymentStatus;
  final PaymentMethod paymentMethod;
  final double totalAmount;
  final bool isPickup;
  final DriverJobAction action;
  final DateTime assignedAt;
  final DateTime? acceptedAt;
  final DateTime? completedAt;
  final String? notes;
  final DateTime? pickupScheduledAt;
  final DateTime? deliveryScheduledAt;
  final DateTime? pickupActualAt;
  final DateTime? deliveryActualAt;
  final String? pickupStreet;
  final String? pickupCityName;
  final String? deliveryStreet;
  final String? deliveryCityName;
  final String? customerName;
  final String? customerPhone;
  final List<String> serviceNames;

  const DriverAssignmentDetailModel({
    required this.id,
    required this.orderId,
    required this.orderNumber,
    required this.status,
    this.paymentStatus = PaymentStatus.pending,
    this.paymentMethod = PaymentMethod.cash,
    required this.totalAmount,
    required this.isPickup,
    required this.action,
    required this.assignedAt,
    this.acceptedAt,
    this.completedAt,
    this.notes,
    this.pickupScheduledAt,
    this.deliveryScheduledAt,
    this.pickupActualAt,
    this.deliveryActualAt,
    this.pickupStreet,
    this.pickupCityName,
    this.deliveryStreet,
    this.deliveryCityName,
    this.customerName,
    this.customerPhone,
    this.serviceNames = const [],
  });

  factory DriverAssignmentDetailModel.fromJson(Map<String, dynamic> json) =>
      DriverAssignmentDetailModel(
        id: json['id'] as int? ?? 0,
        orderId: json['orderId'] as int? ?? 0,
        orderNumber: json['orderNumber'] as String? ?? '',
        status: OrderStatusExtension.fromString(json['status'] as String?),
        paymentStatus: PaymentStatusExtension.fromString(json['paymentStatus'] as String?),
        paymentMethod: PaymentMethodExtension.fromString(json['paymentMethod'] as String?),
        totalAmount: _toDouble(json['totalAmount']),
        isPickup: json['isPickup'] as bool? ?? false,
        action: DriverJobActionExtension.fromString(json['action'] as String?),
        assignedAt: _parseDateTime(json['assignedAt']) ?? DateTime.now(),
        acceptedAt: _parseDateTime(json['acceptedAt']),
        completedAt: _parseDateTime(json['completedAt']),
        notes: json['notes'] as String?,
        pickupScheduledAt: _parseDateTime(json['pickupScheduledAt']),
        deliveryScheduledAt: _parseDateTime(json['deliveryScheduledAt']),
        pickupActualAt: _parseDateTime(json['pickupActualAt']),
        deliveryActualAt: _parseDateTime(json['deliveryActualAt']),
        pickupStreet: json['pickupStreet'] as String?,
        pickupCityName: json['pickupCityName'] as String?,
        deliveryStreet: json['deliveryStreet'] as String?,
        deliveryCityName: json['deliveryCityName'] as String?,
        customerName: json['customerName'] as String?,
        customerPhone: json['customerPhone'] as String?,
        serviceNames: (json['serviceNames'] as List<dynamic>?)
                ?.map((e) => e as String)
                .toList() ??
            [],
      );

  bool get isCompleted => action == DriverJobAction.completed;
  bool get canAccept => action == DriverJobAction.pending;
  bool get canComplete =>
      action == DriverJobAction.accepted;
  bool get canCollectPayment =>
      !isPickup && action == DriverJobAction.completed && paymentStatus != PaymentStatus.paid;
  bool get isPaymentCollected => paymentStatus == PaymentStatus.paid;

  @override
  List<Object?> get props => [
        id,
        orderId,
        orderNumber,
        status,
        paymentStatus,
        paymentMethod,
        totalAmount,
        isPickup,
        action,
        assignedAt,
        acceptedAt,
        completedAt,
        notes,
        pickupScheduledAt,
        deliveryScheduledAt,
        pickupActualAt,
        deliveryActualAt,
        pickupStreet,
        pickupCityName,
        deliveryStreet,
        deliveryCityName,
        customerName,
        customerPhone,
        serviceNames,
      ];
}

double _toDouble(dynamic value) {
  if (value == null) return 0;
  if (value is double) return value;
  if (value is int) return value.toDouble();
  if (value is String) return double.tryParse(value) ?? 0;
  return 0;
}

DateTime? _parseDateTime(dynamic value) {
  if (value == null) return null;
  if (value is DateTime) return value;
  return DateTime.tryParse(value.toString());
}