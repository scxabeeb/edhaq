import 'package:dio/dio.dart';
import '../constants/app_constants.dart';
import '../data/models/address_models.dart';
import '../data/models/auth_request_models.dart';
import '../data/models/dashboard_model.dart';
import '../data/models/location_models.dart';
import '../data/models/notification_model.dart';
import '../data/models/order_models.dart';
import '../data/models/service_models.dart';
import '../data/models/user_model.dart';/// Thin wrapper around [Dio] that exposes typed methods for every
/// backend API endpoint.
///
/// The [DioClient] (in the same directory) is responsible for base URL,
/// timeouts, and the auth-token interceptor.  This class only concerns
/// itself with endpoint paths, request/response (de)serialization, and
/// error mapping.
class ApiService {
  final Dio _dio;

  ApiService(this._dio);

  // ════════════════════════════════════════════════════════════════════════
  //  Auth endpoints  —  /api/auth
  // ════════════════════════════════════════════════════════════════════════

  Future<LoginResponse> login(LoginRequest request) async {
    final response =
        await _post('${AppConstants.authPath}/login', data: request.toJson());
    return LoginResponse.fromJson(_toMap(response.data));
  }

  Future<LoginResponse> register(RegisterRequest request) async {
    final response =
        await _post('${AppConstants.authPath}/register', data: request.toJson());
    return LoginResponse.fromJson(_toMap(response.data));
  }

  Future<AppUser> getCurrentUser() async {
    final response = await _get('${AppConstants.authPath}/me');
    return AppUser.fromJson(_toMap(response.data));
  }

  Future<void> logout() async {
    await _post('${AppConstants.authPath}/logout');
  }

  // ════════════════════════════════════════════════════════════════════════
  //  Location endpoints  —  /api/locations
  // ════════════════════════════════════════════════════════════════════════

  Future<List<CityModel>> getCities() async {
    final response =
        await _get('${AppConstants.locationsPath}/cities');
    return _fromList(response.data, CityModel.fromJson);
  }

  Future<List<VillageModel>> getVillages(int cityId) async {
    final response = await _get(
      '${AppConstants.locationsPath}/cities/$cityId/villages',
    );
    return _fromList(response.data, VillageModel.fromJson);
  }

  Future<List<SubVillageModel>> getSubVillages(int villageId) async {
    final response = await _get(
      '${AppConstants.locationsPath}/villages/$villageId/subvillages',
    );
    return _fromList(response.data, SubVillageModel.fromJson);
  }

  // ════════════════════════════════════════════════════════════════════════
  //  Service endpoints  —  /api/services
  // ════════════════════════════════════════════════════════════════════════

  Future<List<ServiceCategoryModel>> getServiceCategories() async {
    final response =
        await _get('${AppConstants.servicesPath}/categories');
    return _fromList(response.data, ServiceCategoryModel.fromJson);
  }

  Future<List<ServiceModel>> getServices({int? categoryId}) async {
    final response = await _get(
      AppConstants.servicesPath,
      queryParameters: categoryId != null ? {'categoryId': categoryId} : null,
    );
    return _fromList(response.data, ServiceModel.fromJson);
  }

  // ════════════════════════════════════════════════════════════════════════
  //  Address endpoints  —  /api/addresses
  // ════════════════════════════════════════════════════════════════════════

  Future<List<AddressModel>> getAddresses() async {
    final response = await _get(AppConstants.addressesPath);
    return _fromList(response.data, AddressModel.fromJson);
  }

  Future<AddressModel> createAddress(CreateAddressRequest request) async {
    final response = await _post(
      AppConstants.addressesPath,
      data: request.toJson(),
    );
    return AddressModel.fromJson(_toMap(response.data));
  }

  Future<void> deleteAddress(int id) async {
    await _delete('${AppConstants.addressesPath}/$id');
  }

  // ════════════════════════════════════════════════════════════════════════
  //  Order endpoints  —  /api/orders
  // ════════════════════════════════════════════════════════════════════════

  Future<PagedOrdersResponse> getOrders({
    int page = 1,
    int pageSize = 10,
  }) async {
    final response = await _get(
      AppConstants.ordersPath,
      queryParameters: {'page': page, 'pageSize': pageSize},
    );
    return PagedOrdersResponse.fromJson(_toMap(response.data));
  }

  Future<OrderDetailModel> getOrder(int id) async {
    final response = await _get('${AppConstants.ordersPath}/$id');
    return OrderDetailModel.fromJson(_toMap(response.data));
  }

  Future<OrderSummaryModel> createOrder(CreateOrderRequest request) async {
    final response = await _post(
      AppConstants.ordersPath,
      data: request.toJson(),
    );
    return OrderSummaryModel.fromJson(_toMap(response.data));
  }

  Future<OrderDetailModel> confirmDelivery(int orderId) async {
    final response = await _post(
      '${AppConstants.ordersPath}/$orderId/confirm-delivery',
    );
    return OrderDetailModel.fromJson(_toMap(response.data));
  }

  /// Marks an order as paid after the customer dials the USSD payment code
  /// (*884*442628*amount#). [transactionReference] is optional.
  Future<void> payOrder(int orderId, {String? transactionReference}) async {
    await _post(
      '${AppConstants.ordersPath}/$orderId/pay',
      data: transactionReference == null
          ? null
          : {'transactionReference': transactionReference},
    );
  }

  // ════════════════════════════════════════════════════════════════════════
  //  Notification endpoints  —  /api/notifications
  // ════════════════════════════════════════════════════════════════════════

  Future<List<NotificationModel>> getNotifications({
    bool unreadOnly = true,
  }) async {
    final response = await _get(
      AppConstants.notificationsPath,
      queryParameters: {'unreadOnly': unreadOnly},
    );
    return _fromList(response.data, NotificationModel.fromJson);
  }

  Future<void> markNotificationAsRead(int id) async {
    await _post('${AppConstants.notificationsPath}/$id/read');
  }

  // ════════════════════════════════════════════════════════════════════════
  //  Dashboard endpoints  —  /api/dashboard
  // ════════════════════════════════════════════════════════════════════════

  Future<CustomerDashboardModel> getCustomerDashboard() async {
    final response =
        await _get('${AppConstants.dashboardPath}/customer');
    return CustomerDashboardModel.fromJson(_toMap(response.data));
  }

  Future<DriverDashboardModel> getDriverDashboard() async {
    final response =
        await _get('${AppConstants.dashboardPath}/driver');
    return DriverDashboardModel.fromJson(_toMap(response.data));
  }

  Future<AdminDashboardModel> getAdminDashboard() async {
    final response =
        await _get('${AppConstants.dashboardPath}/admin');
    return AdminDashboardModel.fromJson(_toMap(response.data));
  }

  // ════════════════════════════════════════════════════════════════════════
  //  Driver endpoints  —  /api/orders/driver
  // ════════════════════════════════════════════════════════════════════════

  Future<List<DriverAssignmentDetailModel>> getDriverAssignments({
    int page = 1,
    int pageSize = 20,
    DriverJobAction? statusFilter,
    bool? isPickupFilter,
  }) async {
    final params = <String, dynamic>{
      'page': page,
      'pageSize': pageSize,
      if (statusFilter != null)
        // ignore: use_null_aware_elements
        'statusFilter': _driverJobActionToString(statusFilter),
      // ignore: use_null_aware_elements
      if (isPickupFilter != null) 'isPickupFilter': isPickupFilter,
    };
    final response = await _get(
      '${AppConstants.ordersPath}/driver/assignments',
      queryParameters: params,
    );
    return _fromList(response.data, DriverAssignmentDetailModel.fromJson);
  }

  Future<void> acceptDriverAssignment(int assignmentId) async {
    await _post(
        '${AppConstants.ordersPath}/driver/assignments/$assignmentId/accept');
  }

  Future<void> notifyOnTheWay(int assignmentId) async {
    await _post(
        '${AppConstants.ordersPath}/driver/assignments/$assignmentId/on-the-way');
  }

  Future<void> notifyAtGate(int assignmentId) async {
    await _post(
        '${AppConstants.ordersPath}/driver/assignments/$assignmentId/at-gate');
  }

  Future<void> completeDriverAssignment(int assignmentId) async {
    await _post(
        '${AppConstants.ordersPath}/driver/assignments/$assignmentId/complete');
  }

  Future<void> collectPaymentForAssignment(int assignmentId) async {
    await _post(
        '${AppConstants.ordersPath}/driver/assignments/$assignmentId/collect-payment');
  }

  // ════════════════════════════════════════════════════════════════════════
  //  Admin endpoints  —  /api/orders/admin
  // ════════════════════════════════════════════════════════════════════════

  Future<PagedOrdersResponse> getAdminOrders({
    int page = 1,
    int pageSize = 20,
    String? search,
    OrderStatus? statusFilter,
    bool? activeOnly,
    DateTime? dateFrom,
    DateTime? dateTo,
  }) async {
    final params = <String, dynamic>{
      'page': page,
      'pageSize': pageSize,
      // ignore: use_null_aware_elements
      if (search != null)
        // ignore: use_null_aware_elements
        'search': search,
      if (statusFilter != null)
        // ignore: use_null_aware_elements
        'statusFilter': _orderStatusToString(statusFilter),
      // ignore: use_null_aware_elements
      if (activeOnly != null) 'activeOnly': activeOnly,
      if (dateFrom != null)
        // ignore: use_null_aware_elements
        'dateFrom': dateFrom.toIso8601String(),
      if (dateTo != null)
        // ignore: use_null_aware_elements
        'dateTo': dateTo.toIso8601String(),
    };
    final response = await _get(
      '${AppConstants.ordersPath}/admin',
      queryParameters: params,
    );
    return PagedOrdersResponse.fromJson(_toMap(response.data));
  }

  Future<void> updateOrderStatus(int orderId, UpdateOrderStatusRequest request) async {
    await _post(
      '${AppConstants.ordersPath}/admin/$orderId/status',
      data: request.toJson(),
    );
  }

  Future<void> assignDriver(AssignDriverRequest request) async {
    await _post(
      '${AppConstants.ordersPath}/admin/assign-driver',
      data: request.toJson(),
    );
  }

  // ════════════════════════════════════════════════════════════════════════
  //  Private helpers
  // ════════════════════════════════════════════════════════════════════════

  String _driverJobActionToString(DriverJobAction action) =>
      action.toString().split('.').last;

  String _orderStatusToString(OrderStatus status) =>
      status.toString().split('.').last;

  // ════════════════════════════════════════════════════════════════════════
  //  Private helpers
  // ════════════════════════════════════════════════════════════════════════

  Future<Response<T>> _get<T>(
    String path, {
    Map<String, dynamic>? queryParameters,
  }) =>
      _dio.get<T>(path, queryParameters: queryParameters);

  Future<Response<T>> _post<T>(
    String path, {
    Object? data,
    Map<String, dynamic>? queryParameters,
  }) =>
      _dio.post<T>(path, data: data, queryParameters: queryParameters);

  Future<Response<T>> _delete<T>(
    String path, {
    Object? data,
  }) =>
      _dio.delete<T>(path, data: data);

  /// Safely converts a [dynamic] JSON body to a [Map<String, dynamic>].
  Map<String, dynamic> _toMap(dynamic data) {
    if (data is Map<String, dynamic>) return data;
    if (data is Map) return Map<String, dynamic>.from(data);
    return {};
  }

  /// Safely converts a list JSON body into a typed list.
  List<T> _fromList<T>(dynamic data, T Function(Map<String, dynamic>) fromJson) {
    if (data is! List) return [];
    return data
        .whereType<Map>()
        .map((e) => fromJson(Map<String, dynamic>.from(e)))
        .toList();
  }
}
