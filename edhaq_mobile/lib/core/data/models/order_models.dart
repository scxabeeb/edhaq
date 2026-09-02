import 'package:equatable/equatable.dart';

/// Order lifecycle status — mirrors backend [OrderStatus] enum.
enum OrderStatus {
  orderPlaced,
  pickupScheduled,
  driverAssigned,
  driverOnTheWay,
  clothesPickedUp,
  laundryReceived,
  sorting,
  washing,
  dryCleaning,
  drying,
  ironing,
  folding,
  packaging,
  readyForDelivery,
  outForDelivery,
  delivered,
  completed,
  cancelled,
  customerConfirmed,
  unknown,
}

extension OrderStatusExtension on OrderStatus {
  String get displayName {
    switch (this) {
      case OrderStatus.orderPlaced:
        return 'Order Placed';
      case OrderStatus.pickupScheduled:
        return 'Pickup Scheduled';
      case OrderStatus.driverAssigned:
        return 'Driver Assigned';
      case OrderStatus.driverOnTheWay:
        return 'Driver on the Way';
      case OrderStatus.clothesPickedUp:
        return 'Clothes Picked Up';
      case OrderStatus.laundryReceived:
        return 'Laundry Received';
      case OrderStatus.sorting:
        return 'Sorting';
      case OrderStatus.washing:
        return 'Washing';
      case OrderStatus.dryCleaning:
        return 'Dry Cleaning';
      case OrderStatus.drying:
        return 'Drying';
      case OrderStatus.ironing:
        return 'Ironing';
      case OrderStatus.folding:
        return 'Folding';
      case OrderStatus.packaging:
        return 'Packaging';
      case OrderStatus.readyForDelivery:
        return 'Ready for Delivery';
      case OrderStatus.outForDelivery:
        return 'Out for Delivery';
      case OrderStatus.delivered:
        return 'Delivered';
      case OrderStatus.completed:
        return 'Completed';
      case OrderStatus.cancelled:
        return 'Cancelled';
      case OrderStatus.customerConfirmed:
        return 'Customer Confirmed';
      case OrderStatus.unknown:
        return 'Unknown';
    }
  }

  /// Whether the status is part of the active (non-terminal) lifecycle.
  bool get isTerminal =>
      this == OrderStatus.completed ||
      this == OrderStatus.cancelled ||
      this == OrderStatus.customerConfirmed;

  static OrderStatus fromString(String? value) {
    if (value == null || value.isEmpty) return OrderStatus.unknown;
    return switch (value) {
      'OrderPlaced' => OrderStatus.orderPlaced,
      'PickupScheduled' => OrderStatus.pickupScheduled,
      'DriverAssigned' => OrderStatus.driverAssigned,
      'DriverOnTheWay' => OrderStatus.driverOnTheWay,
      'ClothesPickedUp' => OrderStatus.clothesPickedUp,
      'LaundryReceived' => OrderStatus.laundryReceived,
      'Sorting' => OrderStatus.sorting,
      'Washing' => OrderStatus.washing,
      'DryCleaning' => OrderStatus.dryCleaning,
      'Drying' => OrderStatus.drying,
      'Ironing' => OrderStatus.ironing,
      'Folding' => OrderStatus.folding,
      'Packaging' => OrderStatus.packaging,
      'ReadyForDelivery' => OrderStatus.readyForDelivery,
      'OutForDelivery' => OrderStatus.outForDelivery,
      'Delivered' => OrderStatus.delivered,
      'Completed' => OrderStatus.completed,
      'Cancelled' => OrderStatus.cancelled,
      'CustomerConfirmed' => OrderStatus.customerConfirmed,
      _ => OrderStatus.unknown,
    };
  }
}

/// Payment method — mirrors backend [PaymentMethod] enum.
enum PaymentMethod {
  cash,
  evcPlus,
  zaad,
  eDahab,
  sahal,
  myCash,
  visa,
  masterCard,
  unknown,
}

extension PaymentMethodExtension on PaymentMethod {
  String get displayName {
    switch (this) {
      case PaymentMethod.cash:
        return 'Cash';
      case PaymentMethod.evcPlus:
        return 'EVC Plus';
      case PaymentMethod.zaad:
        return 'ZAAD';
      case PaymentMethod.eDahab:
        return 'eDahab';
      case PaymentMethod.sahal:
        return 'Sahal';
      case PaymentMethod.myCash:
        return 'MyCash';
      case PaymentMethod.visa:
        return 'Visa';
      case PaymentMethod.masterCard:
        return 'Mastercard';
      case PaymentMethod.unknown:
        return 'Unknown';
    }
  }

  static PaymentMethod fromString(String? value) {
    if (value == null || value.isEmpty) return PaymentMethod.unknown;
    return switch (value) {
      'Cash' => PaymentMethod.cash,
      'EVCPlus' => PaymentMethod.evcPlus,
      'ZAAD' => PaymentMethod.zaad,
      'EDahab' => PaymentMethod.eDahab,
      'Sahal' => PaymentMethod.sahal,
      'MyCash' => PaymentMethod.myCash,
      'Visa' => PaymentMethod.visa,
      'MasterCard' => PaymentMethod.masterCard,
      _ => PaymentMethod.unknown,
    };
  }
}

/// Payment status — mirrors backend [PaymentStatus] enum.
enum PaymentStatus {
  pending,
  paid,
  failed,
  refunded,
  unknown,
}

extension PaymentStatusExtension on PaymentStatus {
  String get displayName {
    switch (this) {
      case PaymentStatus.pending:
        return 'Pending';
      case PaymentStatus.paid:
        return 'Paid';
      case PaymentStatus.failed:
        return 'Failed';
      case PaymentStatus.refunded:
        return 'Refunded';
      case PaymentStatus.unknown:
        return 'Unknown';
    }
  }

  static PaymentStatus fromString(String? value) {
    if (value == null || value.isEmpty) return PaymentStatus.unknown;
    return switch (value) {
      'Pending' => PaymentStatus.pending,
      'Paid' => PaymentStatus.paid,
      'Failed' => PaymentStatus.failed,
      'Refunded' => PaymentStatus.refunded,
      _ => PaymentStatus.unknown,
    };
  }
}

/// Driver's assignment status — mirrors backend [DriverJobAction] enum.
enum DriverJobAction {
  pending,
  accepted,
  rejected,
  completed,
  unknown,
}

extension DriverJobActionExtension on DriverJobAction {
  String get displayName {
    switch (this) {
      case DriverJobAction.pending:
        return 'Pending';
      case DriverJobAction.accepted:
        return 'Accepted';
      case DriverJobAction.rejected:
        return 'Rejected';
      case DriverJobAction.completed:
        return 'Completed';
      default:
        return 'Unknown';
    }
  }

  static DriverJobAction fromString(String? value) {
    if (value == null || value.isEmpty) return DriverJobAction.unknown;
    return switch (value) {
      'Pending' => DriverJobAction.pending,
      'Accepted' => DriverJobAction.accepted,
      'Rejected' => DriverJobAction.rejected,
      'Completed' => DriverJobAction.completed,
      _ => DriverJobAction.unknown,
    };
  }
}

// ── Order item ──────────────────────────────────────────────────────────

class OrderItemModel extends Equatable {
  final int serviceId;
  final String? serviceName;
  final String? categoryName;
  final int quantity;
  final double unitPrice;
  final double totalPrice;
  final String? notes;

  const OrderItemModel({
    required this.serviceId,
    this.serviceName,
    this.categoryName,
    required this.quantity,
    required this.unitPrice,
    required this.totalPrice,
    this.notes,
  });

  factory OrderItemModel.fromJson(Map<String, dynamic> json) => OrderItemModel(
        serviceId: json['serviceId'] as int? ?? 0,
        serviceName: json['serviceName'] as String?,
        categoryName: json['categoryName'] as String?,
        quantity: json['quantity'] as int? ?? 0,
        unitPrice: _toDouble(json['unitPrice']),
        totalPrice: _toDouble(json['totalPrice']),
        notes: json['notes'] as String?,
      );

  Map<String, dynamic> toJson() => {
        'serviceId': serviceId,
        'serviceName': serviceName,
        'categoryName': categoryName,
        'quantity': quantity,
        'unitPrice': unitPrice,
        'totalPrice': totalPrice,
        'notes': notes,
      };

  @override
  List<Object?> get props =>
      [serviceId, serviceName, categoryName, quantity, unitPrice, totalPrice, notes];
}

// ── Order tracking entry ────────────────────────────────────────────────

class OrderTrackingModel extends Equatable {
  final OrderStatus status;
  final String? note;
  final String? updatedByName;
  final double? driverLatitude;
  final double? driverLongitude;
  final DateTime createdAt;

  const OrderTrackingModel({
    required this.status,
    this.note,
    this.updatedByName,
    this.driverLatitude,
    this.driverLongitude,
    required this.createdAt,
  });

  factory OrderTrackingModel.fromJson(Map<String, dynamic> json) =>
      OrderTrackingModel(
        status: OrderStatusExtension.fromString(json['status'] as String?),
        note: json['note'] as String?,
        updatedByName: json['updatedByName'] as String?,
        driverLatitude: _toDoubleOrNull(json['driverLatitude']),
        driverLongitude: _toDoubleOrNull(json['driverLongitude']),
        createdAt: _parseDateTime(json['createdAt']) ?? DateTime.now(),
      );

  @override
  List<Object?> get props =>
      [status, note, updatedByName, driverLatitude, driverLongitude, createdAt];
}

// ── Driver assignment ───────────────────────────────────────────────────

class DriverAssignmentModel extends Equatable {
  final int driverId;
  final String? driverName;
  final String? phoneNumber;
  final String? vehicleModel;
  final String? licensePlate;
  final bool isPickup;
  final DriverJobAction status;
  final DateTime assignedAt;

  const DriverAssignmentModel({
    required this.driverId,
    this.driverName,
    this.phoneNumber,
    this.vehicleModel,
    this.licensePlate,
    required this.isPickup,
    required this.status,
    required this.assignedAt,
  });

  factory DriverAssignmentModel.fromJson(Map<String, dynamic> json) =>
      DriverAssignmentModel(
        driverId: json['driverId'] as int? ?? 0,
        driverName: json['driverName'] as String?,
        phoneNumber: json['phoneNumber'] as String?,
        vehicleModel: json['vehicleModel'] as String?,
        licensePlate: json['licensePlate'] as String?,
        isPickup: json['isPickup'] as bool? ?? false,
        status: DriverJobActionExtension.fromString(json['status'] as String?),
        assignedAt: _parseDateTime(json['assignedAt']) ?? DateTime.now(),
      );

  @override
  List<Object?> get props => [
        driverId,
        driverName,
        phoneNumber,
        vehicleModel,
        licensePlate,
        isPickup,
        status,
        assignedAt,
      ];
}

// ── Address summary (inside order) ──────────────────────────────────────

class AddressSummaryModel extends Equatable {
  final int id;
  final String label;
  final String street;
  final String? district;
  final String? cityName;
  final String? villageName;
  final String? subVillageName;

  const AddressSummaryModel({
    required this.id,
    required this.label,
    required this.street,
    this.district,
    this.cityName,
    this.villageName,
    this.subVillageName,
  });

  factory AddressSummaryModel.fromJson(Map<String, dynamic> json) =>
      AddressSummaryModel(
        id: json['id'] as int? ?? 0,
        label: json['label'] as String? ?? '',
        street: json['street'] as String? ?? '',
        district: json['district'] as String?,
        cityName: json['cityName'] as String?,
        villageName: json['villageName'] as String?,
        subVillageName: json['subVillageName'] as String?,
      );

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

  @override
  List<Object?> get props =>
      [id, label, street, district, cityName, villageName, subVillageName];
}

// ── Order summary (list item) ───────────────────────────────────────────

class OrderSummaryModel extends Equatable {
  final int id;
  final String orderNumber;
  final OrderStatus status;
  final double totalAmount;
  final DateTime createdAt;
  final DateTime estimatedCompletionAt;

  const OrderSummaryModel({
    required this.id,
    required this.orderNumber,
    required this.status,
    required this.totalAmount,
    required this.createdAt,
    required this.estimatedCompletionAt,
  });

  factory OrderSummaryModel.fromJson(Map<String, dynamic> json) =>
      OrderSummaryModel(
        id: json['id'] as int? ?? 0,
        orderNumber: json['orderNumber'] as String? ?? '',
        status: OrderStatusExtension.fromString(json['status'] as String?),
        totalAmount: _toDouble(json['totalAmount']),
        createdAt: _parseDateTime(json['createdAt']) ?? DateTime.now(),
        estimatedCompletionAt:
            _parseDateTime(json['estimatedCompletionAt']) ?? DateTime.now(),
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'orderNumber': orderNumber,
        'status': status.toString().split('.').last,
        'totalAmount': totalAmount,
        'createdAt': createdAt.toIso8601String(),
        'estimatedCompletionAt': estimatedCompletionAt.toIso8601String(),
      };

  @override
  List<Object?> get props =>
      [id, orderNumber, status, totalAmount, createdAt, estimatedCompletionAt];
}

// ── Order detail ────────────────────────────────────────────────────────

class OrderDetailModel extends Equatable {
  final int id;
  final String orderNumber;
  final OrderStatus status;
  final PaymentStatus paymentStatus;
  final PaymentMethod paymentMethod;
  final double subTotal;
  final double deliveryFee;
  final double discount;
  final double totalAmount;
  final String? specialInstructions;
  final DateTime pickupScheduledAt;
  final DateTime? pickupActualAt;
  final DateTime deliveryScheduledAt;
  final DateTime? deliveryActualAt;
  final DateTime estimatedCompletionAt;
  final DateTime createdAt;
  final DateTime? updatedAt;
  final AddressSummaryModel? pickupAddress;
  final AddressSummaryModel? deliveryAddress;
  final List<OrderItemModel> items;
  final List<OrderTrackingModel> trackings;
  final List<DriverAssignmentModel> driverAssignments;
  final String? qrCodeBase64;
  final String? barcodeValue;

  const OrderDetailModel({
    required this.id,
    required this.orderNumber,
    required this.status,
    required this.paymentStatus,
    required this.paymentMethod,
    required this.subTotal,
    required this.deliveryFee,
    required this.discount,
    required this.totalAmount,
    this.specialInstructions,
    required this.pickupScheduledAt,
    this.pickupActualAt,
    required this.deliveryScheduledAt,
    this.deliveryActualAt,
    required this.estimatedCompletionAt,
    required this.createdAt,
    this.updatedAt,
    this.pickupAddress,
    this.deliveryAddress,
    this.items = const [],
    this.trackings = const [],
    this.driverAssignments = const [],
    this.qrCodeBase64,
    this.barcodeValue,
  });

  factory OrderDetailModel.fromJson(Map<String, dynamic> json) =>
      OrderDetailModel(
        id: json['id'] as int? ?? 0,
        orderNumber: json['orderNumber'] as String? ?? '',
        status: OrderStatusExtension.fromString(json['status'] as String?),
        paymentStatus:
            PaymentStatusExtension.fromString(json['paymentStatus'] as String?),
        paymentMethod:
            PaymentMethodExtension.fromString(json['paymentMethod'] as String?),
        subTotal: _toDouble(json['subTotal']),
        // Delivery fee is removed everywhere — always zero in the app.
        deliveryFee: 0,
        discount: _toDouble(json['discount']),
        totalAmount: _toDouble(json['totalAmount']),
        specialInstructions: json['specialInstructions'] as String?,
        pickupScheduledAt:
            _parseDateTime(json['pickupScheduledAt']) ?? DateTime.now(),
        pickupActualAt: _parseDateTime(json['pickupActualAt']),
        deliveryScheduledAt:
            _parseDateTime(json['deliveryScheduledAt']) ?? DateTime.now(),
        deliveryActualAt: _parseDateTime(json['deliveryActualAt']),
        estimatedCompletionAt:
            _parseDateTime(json['estimatedCompletionAt']) ?? DateTime.now(),
        createdAt: _parseDateTime(json['createdAt']) ?? DateTime.now(),
        updatedAt: _parseDateTime(json['updatedAt']),
        pickupAddress: json['pickupAddress'] is Map<String, dynamic>
            ? AddressSummaryModel.fromJson(
                Map<String, dynamic>.from(json['pickupAddress'] as Map))
            : null,
        deliveryAddress: json['deliveryAddress'] is Map<String, dynamic>
            ? AddressSummaryModel.fromJson(
                Map<String, dynamic>.from(json['deliveryAddress'] as Map))
            : null,
        items: (json['items'] as List<dynamic>?)
                ?.map((e) =>
                    OrderItemModel.fromJson(Map<String, dynamic>.from(e)))
                .toList() ??
            [],
        trackings: (json['trackings'] as List<dynamic>?)
                ?.map((e) =>
                    OrderTrackingModel.fromJson(Map<String, dynamic>.from(e)))
                .toList() ??
            [],
        driverAssignments: (json['driverAssignments'] as List<dynamic>?)
                ?.map((e) =>
                    DriverAssignmentModel.fromJson(Map<String, dynamic>.from(e)))
                .toList() ??
            [],
        qrCodeBase64: json['qrCodeBase64'] as String?,
        barcodeValue: json['barcodeValue'] as String?,
      );

  /// Convenience: latest tracking status or the order's own status.
  OrderStatus get effectiveStatus {
    if (trackings.isNotEmpty) return trackings.first.status;
    return status;
  }

  /// The driver assigned for pickup (if any).
  DriverAssignmentModel? get pickupDriver =>
      driverAssignments.where((d) => d.isPickup).firstOrNull;

  /// The driver assigned for delivery (if any).
  DriverAssignmentModel? get deliveryDriver =>
      driverAssignments.where((d) => !d.isPickup).firstOrNull;

  @override
  List<Object?> get props => [
        id,
        orderNumber,
        status,
        paymentStatus,
        paymentMethod,
        subTotal,
        deliveryFee,
        discount,
        totalAmount,
        specialInstructions,
        pickupScheduledAt,
        pickupActualAt,
        deliveryScheduledAt,
        deliveryActualAt,
        estimatedCompletionAt,
        createdAt,
        updatedAt,
        pickupAddress,
        deliveryAddress,
        items,
        trackings,
        driverAssignments,
        qrCodeBase64,
        barcodeValue,
      ];
}

// ── Create order request ────────────────────────────────────────────────

class CreateOrderItemRequest {
  final int serviceId;
  final int quantity;
  final String? notes;

  const CreateOrderItemRequest({
    required this.serviceId,
    required this.quantity,
    this.notes,
  });

  Map<String, dynamic> toJson() => {
        'serviceId': serviceId,
        'quantity': quantity,
        'notes': notes,
      };
}

class CreateOrderRequest {
  final int pickupAddressId;
  final int deliveryAddressId;
  final DateTime pickupScheduledAt;
  final DateTime deliveryScheduledAt;
  final List<CreateOrderItemRequest> items;
  final String? couponCode;
  final String? specialInstructions;
  final PaymentMethod paymentMethod;

  const CreateOrderRequest({
    required this.pickupAddressId,
    required this.deliveryAddressId,
    required this.pickupScheduledAt,
    required this.deliveryScheduledAt,
    required this.items,
    this.couponCode,
    this.specialInstructions,
    this.paymentMethod = PaymentMethod.cash,
  });

  Map<String, dynamic> toJson() => {
        'pickupAddressId': pickupAddressId,
        'deliveryAddressId': deliveryAddressId,
        'pickupScheduledAt': pickupScheduledAt.toIso8601String(),
        'deliveryScheduledAt': deliveryScheduledAt.toIso8601String(),
        'items': items.map((i) => i.toJson()).toList(),
        'couponCode': couponCode,
        'specialInstructions': specialInstructions,
        'paymentMethod': paymentMethod.toString().split('.').last,
      };
}

class UpdateOrderStatusRequest {
  final OrderStatus status;
  final String? note;

  const UpdateOrderStatusRequest({
    required this.status,
    this.note,
  });

  Map<String, dynamic> toJson() => {
        'status': status.toString().split('.').last,
        'note': note,
      };
}

class AssignDriverRequest {
  final int orderId;
  final int driverId;

  const AssignDriverRequest({
    required this.orderId,
    required this.driverId,
  });

  Map<String, dynamic> toJson() => {
        'orderId': orderId,
        'driverId': driverId,
      };
}

// ── Paged orders response ───────────────────────────────────────────────

class PagedOrdersResponse extends Equatable {
  final int totalCount;
  final int page;
  final int pageSize;
  final int totalPages;
  final List<OrderSummaryModel> orders;

  const PagedOrdersResponse({
    required this.totalCount,
    required this.page,
    required this.pageSize,
    required this.totalPages,
    this.orders = const [],
  });

  factory PagedOrdersResponse.fromJson(Map<String, dynamic> json) =>
      PagedOrdersResponse(
        totalCount: json['totalCount'] as int? ?? 0,
        page: json['page'] as int? ?? 1,
        pageSize: json['pageSize'] as int? ?? 10,
        totalPages: json['totalPages'] as int? ?? 0,
        orders: (json['orders'] as List<dynamic>?)
                ?.map((e) =>
                    OrderSummaryModel.fromJson(Map<String, dynamic>.from(e)))
                .toList() ??
            [],
      );

  @override
  List<Object?> get props =>
      [totalCount, page, pageSize, totalPages, orders];
}

// ── Helpers ─────────────────────────────────────────────────────────────

double _toDouble(dynamic value) {
  if (value == null) return 0;
  if (value is double) return value;
  if (value is int) return value.toDouble();
  if (value is String) return double.tryParse(value) ?? 0;
  return 0;
}

double? _toDoubleOrNull(dynamic value) {
  if (value == null) return null;
  if (value is double) return value;
  if (value is int) return value.toDouble();
  if (value is String) return double.tryParse(value);
  return null;
}

DateTime? _parseDateTime(dynamic value) {
  if (value == null) return null;
  if (value is DateTime) return value;
  return DateTime.tryParse(value.toString());
}
