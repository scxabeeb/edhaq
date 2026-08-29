import 'package:equatable/equatable.dart';

/// Notification category — mirrors backend [NotificationType] enum.
enum NotificationType {
  orderCreated,
  pickupScheduled,
  driverAssigned,
  laundryStarted,
  laundryReady,
  outForDelivery,
  delivered,
  paymentConfirmed,
  general,
  unknown,
}

extension NotificationTypeExtension on NotificationType {
  String get displayName {
    switch (this) {
      case NotificationType.orderCreated:
        return 'Order Created';
      case NotificationType.pickupScheduled:
        return 'Pickup Scheduled';
      case NotificationType.driverAssigned:
        return 'Driver Assigned';
      case NotificationType.laundryStarted:
        return 'Laundry Started';
      case NotificationType.laundryReady:
        return 'Laundry Ready';
      case NotificationType.outForDelivery:
        return 'Out for Delivery';
      case NotificationType.delivered:
        return 'Delivered';
      case NotificationType.paymentConfirmed:
        return 'Payment Confirmed';
      case NotificationType.general:
        return 'General';
      case NotificationType.unknown:
        return 'Unknown';
    }
  }

  static NotificationType fromString(String? value) {
    if (value == null || value.isEmpty) return NotificationType.unknown;
    return switch (value) {
      'OrderCreated' => NotificationType.orderCreated,
      'PickupScheduled' => NotificationType.pickupScheduled,
      'DriverAssigned' => NotificationType.driverAssigned,
      'LaundryStarted' => NotificationType.laundryStarted,
      'LaundryReady' => NotificationType.laundryReady,
      'OutForDelivery' => NotificationType.outForDelivery,
      'Delivered' => NotificationType.delivered,
      'PaymentConfirmed' => NotificationType.paymentConfirmed,
      'General' => NotificationType.general,
      _ => NotificationType.unknown,
    };
  }
}

class NotificationModel extends Equatable {
  final int id;
  final String title;
  final String message;
  final NotificationType type;
  final bool isRead;
  final String? actionUrl;
  final int? orderId;
  final DateTime createdAt;
  final DateTime? readAt;

  const NotificationModel({
    required this.id,
    required this.title,
    required this.message,
    required this.type,
    required this.isRead,
    this.actionUrl,
    this.orderId,
    required this.createdAt,
    this.readAt,
  });

  factory NotificationModel.fromJson(Map<String, dynamic> json) =>
      NotificationModel(
        id: json['id'] as int? ?? 0,
        title: json['title'] as String? ?? '',
        message: json['message'] as String? ?? '',
        type: NotificationTypeExtension.fromString(json['type'] as String?),
        isRead: json['isRead'] as bool? ?? false,
        actionUrl: json['actionUrl'] as String?,
        orderId: json['orderId'] as int?,
        createdAt: _parseDateTime(json['createdAt']) ?? DateTime.now(),
        readAt: _parseDateTime(json['readAt']),
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'title': title,
        'message': message,
        'type': type.toString().split('.').last,
        'isRead': isRead,
        'actionUrl': actionUrl,
        'orderId': orderId,
        'createdAt': createdAt.toIso8601String(),
        'readAt': readAt?.toIso8601String(),
      };

  @override
  List<Object?> get props => [
        id,
        title,
        message,
        type,
        isRead,
        actionUrl,
        orderId,
        createdAt,
        readAt,
      ];
}

DateTime? _parseDateTime(dynamic value) {
  if (value == null) return null;
  if (value is DateTime) return value;
  return DateTime.tryParse(value.toString());
}
