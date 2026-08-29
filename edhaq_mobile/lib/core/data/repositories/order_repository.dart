import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart';
import '../../data/errors/failures.dart';
import '../../data/models/dashboard_model.dart';
import '../../data/models/order_models.dart';
import '../../network/api_service.dart';
import '../../network/network_exception_mapper.dart';

abstract class OrderRepository {
  Future<Either<Failure, PagedOrdersResponse>> getOrders({
    int page = 1,
    int pageSize = 10,
  });
  Future<Either<Failure, OrderDetailModel>> getOrder(int id);
  Future<Either<Failure, OrderSummaryModel>> createOrder(CreateOrderRequest request);
  Future<Either<Failure, OrderDetailModel>> confirmDelivery(int orderId);
  Future<Either<Failure, void>> payOrder(int orderId, {String? transactionReference});

  // Driver
  Future<Either<Failure, List<DriverAssignmentDetailModel>>> getDriverAssignments({
    int page = 1,
    int pageSize = 20,
    DriverJobAction? statusFilter,
    bool? isPickupFilter,
  });
  Future<Either<Failure, void>> acceptDriverAssignment(int assignmentId);
  Future<Either<Failure, void>> notifyOnTheWay(int assignmentId);
  Future<Either<Failure, void>> notifyAtGate(int assignmentId);
  Future<Either<Failure, void>> completeDriverAssignment(int assignmentId);
  Future<Either<Failure, void>> collectPaymentForAssignment(int assignmentId);

  // Admin
  Future<Either<Failure, PagedOrdersResponse>> getAdminOrders({
    int page = 1,
    int pageSize = 20,
    String? search,
    OrderStatus? statusFilter,
    bool? activeOnly,
    DateTime? dateFrom,
    DateTime? dateTo,
  });
  Future<Either<Failure, void>> updateOrderStatus(
      int orderId, UpdateOrderStatusRequest request);
  Future<Either<Failure, void>> assignDriver(AssignDriverRequest request);
}

class OrderRepositoryImpl implements OrderRepository {
  final ApiService apiService;

  OrderRepositoryImpl(this.apiService);

  @override
  Future<Either<Failure, PagedOrdersResponse>> getOrders({
    int page = 1,
    int pageSize = 10,
  }) async {
    try {
      final result = await apiService.getOrders(page: page, pageSize: pageSize);
      return Right(result);
    } on DioException catch (e) {
      return Left(mapDioExceptionToFailure(e));
    }
  }

  @override
  Future<Either<Failure, OrderDetailModel>> getOrder(int id) async {
    try {
      final result = await apiService.getOrder(id);
      return Right(result);
    } on DioException catch (e) {
      return Left(mapDioExceptionToFailure(e));
    }
  }

  @override
  Future<Either<Failure, OrderSummaryModel>> createOrder(
      CreateOrderRequest request) async {
    try {
      final result = await apiService.createOrder(request);
      return Right(result);
    } on DioException catch (e) {
      return Left(mapDioExceptionToFailure(e));
    }
  }

  @override
  Future<Either<Failure, OrderDetailModel>> confirmDelivery(int orderId) async {
    try {
      final result = await apiService.confirmDelivery(orderId);
      return Right(result);
    } on DioException catch (e) {
      return Left(mapDioExceptionToFailure(e));
    }
  }

  @override
  Future<Either<Failure, void>> payOrder(int orderId,
      {String? transactionReference}) async {
    try {
      await apiService.payOrder(orderId, transactionReference: transactionReference);
      return const Right(null);
    } on DioException catch (e) {
      return Left(mapDioExceptionToFailure(e));
    }
  }

  @override
  Future<Either<Failure, List<DriverAssignmentDetailModel>>> getDriverAssignments({
    int page = 1,
    int pageSize = 20,
    DriverJobAction? statusFilter,
    bool? isPickupFilter,
  }) async {
    try {
      final result = await apiService.getDriverAssignments(
        page: page,
        pageSize: pageSize,
        statusFilter: statusFilter,
        isPickupFilter: isPickupFilter,
      );
      return Right(result);
    } on DioException catch (e) {
      return Left(mapDioExceptionToFailure(e));
    }
  }

  @override
  Future<Either<Failure, void>> acceptDriverAssignment(int assignmentId) async {
    try {
      await apiService.acceptDriverAssignment(assignmentId);
      return const Right(null);
    } on DioException catch (e) {
      return Left(mapDioExceptionToFailure(e));
    }
  }

  @override
  Future<Either<Failure, void>> notifyOnTheWay(int assignmentId) async {
    try {
      await apiService.notifyOnTheWay(assignmentId);
      return const Right(null);
    } on DioException catch (e) {
      return Left(mapDioExceptionToFailure(e));
    }
  }

  @override
  Future<Either<Failure, void>> notifyAtGate(int assignmentId) async {
    try {
      await apiService.notifyAtGate(assignmentId);
      return const Right(null);
    } on DioException catch (e) {
      return Left(mapDioExceptionToFailure(e));
    }
  }

  @override
  Future<Either<Failure, void>> completeDriverAssignment(int assignmentId) async {
    try {
      await apiService.completeDriverAssignment(assignmentId);
      return const Right(null);
    } on DioException catch (e) {
      return Left(mapDioExceptionToFailure(e));
    }
  }

  @override
  Future<Either<Failure, void>> collectPaymentForAssignment(int assignmentId) async {
    try {
      await apiService.collectPaymentForAssignment(assignmentId);
      return const Right(null);
    } on DioException catch (e) {
      return Left(mapDioExceptionToFailure(e));
    }
  }

  @override
  Future<Either<Failure, PagedOrdersResponse>> getAdminOrders({
    int page = 1,
    int pageSize = 20,
    String? search,
    OrderStatus? statusFilter,
    bool? activeOnly,
    DateTime? dateFrom,
    DateTime? dateTo,
  }) async {
    try {
      final result = await apiService.getAdminOrders(
        page: page,
        pageSize: pageSize,
        search: search,
        statusFilter: statusFilter,
        activeOnly: activeOnly,
        dateFrom: dateFrom,
        dateTo: dateTo,
      );
      return Right(result);
    } on DioException catch (e) {
      return Left(mapDioExceptionToFailure(e));
    }
  }

  @override
  Future<Either<Failure, void>> updateOrderStatus(
      int orderId, UpdateOrderStatusRequest request) async {
    try {
      await apiService.updateOrderStatus(orderId, request);
      return const Right(null);
    } on DioException catch (e) {
      return Left(mapDioExceptionToFailure(e));
    }
  }

  @override
  Future<Either<Failure, void>> assignDriver(AssignDriverRequest request) async {
    try {
      await apiService.assignDriver(request);
      return const Right(null);
    } on DioException catch (e) {
      return Left(mapDioExceptionToFailure(e));
    }
  }
}
