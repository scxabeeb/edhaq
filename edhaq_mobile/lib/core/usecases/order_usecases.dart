import 'package:dartz/dartz.dart';
import '../data/errors/failures.dart';
import '../data/models/dashboard_model.dart';
import '../data/models/order_models.dart';
import '../data/repositories/order_repository.dart';
import 'usecase.dart';

/// Parameters for fetching a paged list of the current customer's orders.
class GetOrdersParams {
  final int page;
  final int pageSize;

  const GetOrdersParams({this.page = 1, this.pageSize = 10});
}

/// Fetches a paged list of the current customer's orders.
class GetOrdersUseCase
    implements Usecase<PagedOrdersResponse, GetOrdersParams> {
  final OrderRepository repository;

  GetOrdersUseCase(this.repository);

  @override
  Future<Either<Failure, PagedOrdersResponse>> call(GetOrdersParams params) =>
      repository.getOrders(page: params.page, pageSize: params.pageSize);
}

/// Fetches a single order's full details.
class GetOrderUseCase implements Usecase<OrderDetailModel, int> {
  final OrderRepository repository;

  GetOrderUseCase(this.repository);

  @override
  Future<Either<Failure, OrderDetailModel>> call(int id) =>
      repository.getOrder(id);
}

/// Creates a new order.
class CreateOrderUseCase
    implements Usecase<OrderSummaryModel, CreateOrderRequest> {
  final OrderRepository repository;

  CreateOrderUseCase(this.repository);

  @override
  Future<Either<Failure, OrderSummaryModel>> call(CreateOrderRequest params) =>
      repository.createOrder(params);
}

/// Marks an order as paid after the customer dials the USSD payment code.
class PayOrderUseCase implements Usecase<void, PayOrderArgs> {
  final OrderRepository repository;

  PayOrderUseCase(this.repository);

  @override
  Future<Either<Failure, void>> call(PayOrderArgs params) =>
      repository.payOrder(params.orderId,
          transactionReference: params.transactionReference);
}

class PayOrderArgs {
  final int orderId;
  final String? transactionReference;

  const PayOrderArgs({required this.orderId, this.transactionReference});
}

/// Confirms delivery of an order.
class ConfirmDeliveryUseCase implements Usecase<OrderDetailModel, int> {
  final OrderRepository repository;

  ConfirmDeliveryUseCase(this.repository);

  @override
  Future<Either<Failure, OrderDetailModel>> call(int orderId) =>
      repository.confirmDelivery(orderId);
}

// ── Driver use cases ────────────────────────────────────────────────────────

/// Parameters for fetching driver assignments.
class GetDriverAssignmentsParams {
  final int page;
  final int pageSize;
  final DriverJobAction? statusFilter;
  final bool? isPickupFilter;

  const GetDriverAssignmentsParams({
    this.page = 1,
    this.pageSize = 20,
    this.statusFilter,
    this.isPickupFilter,
  });
}

/// Fetches the current driver's assignments.
class GetDriverAssignmentsUseCase
    implements Usecase<List<DriverAssignmentDetailModel>, GetDriverAssignmentsParams> {
  final OrderRepository repository;

  GetDriverAssignmentsUseCase(this.repository);

  @override
  Future<Either<Failure, List<DriverAssignmentDetailModel>>> call(
          GetDriverAssignmentsParams params) =>
      repository.getDriverAssignments(
        page: params.page,
        pageSize: params.pageSize,
        statusFilter: params.statusFilter,
        isPickupFilter: params.isPickupFilter,
      );
}

/// Accepts a driver assignment.
class AcceptDriverAssignmentUseCase implements Usecase<void, int> {
  final OrderRepository repository;

  AcceptDriverAssignmentUseCase(this.repository);

  @override
  Future<Either<Failure, void>> call(int assignmentId) =>
      repository.acceptDriverAssignment(assignmentId);
}

/// Notifies the customer that the driver is on the way.
class NotifyOnTheWayUseCase implements Usecase<void, int> {
  final OrderRepository repository;

  NotifyOnTheWayUseCase(this.repository);

  @override
  Future<Either<Failure, void>> call(int assignmentId) =>
      repository.notifyOnTheWay(assignmentId);
}

/// Notifies the customer that the driver has arrived at the gate.
class NotifyAtGateUseCase implements Usecase<void, int> {
  final OrderRepository repository;

  NotifyAtGateUseCase(this.repository);

  @override
  Future<Either<Failure, void>> call(int assignmentId) =>
      repository.notifyAtGate(assignmentId);
}

/// Completes a driver assignment (pickup or delivery).
class CompleteDriverAssignmentUseCase implements Usecase<void, int> {
  final OrderRepository repository;

  CompleteDriverAssignmentUseCase(this.repository);

  @override
  Future<Either<Failure, void>> call(int assignmentId) =>
      repository.completeDriverAssignment(assignmentId);
}

/// Collects payment for a delivery assignment.
class CollectPaymentUseCase implements Usecase<void, int> {
  final OrderRepository repository;

  CollectPaymentUseCase(this.repository);

  @override
  Future<Either<Failure, void>> call(int assignmentId) =>
      repository.collectPaymentForAssignment(assignmentId);
}

// ── Admin use cases ────────────────────────────────────────────────────────

/// Parameters for fetching all orders (admin view).
class GetAdminOrdersParams {
  final int page;
  final int pageSize;
  final String? search;
  final OrderStatus? statusFilter;
  final bool? activeOnly;
  final DateTime? dateFrom;
  final DateTime? dateTo;

  const GetAdminOrdersParams({
    this.page = 1,
    this.pageSize = 20,
    this.search,
    this.statusFilter,
    this.activeOnly,
    this.dateFrom,
    this.dateTo,
  });
}

/// Fetches all orders (admin/manager only).
class GetAdminOrdersUseCase
    implements Usecase<PagedOrdersResponse, GetAdminOrdersParams> {
  final OrderRepository repository;

  GetAdminOrdersUseCase(this.repository);

  @override
  Future<Either<Failure, PagedOrdersResponse>> call(GetAdminOrdersParams params) =>
      repository.getAdminOrders(
        page: params.page,
        pageSize: params.pageSize,
        search: params.search,
        statusFilter: params.statusFilter,
        activeOnly: params.activeOnly,
        dateFrom: params.dateFrom,
        dateTo: params.dateTo,
      );
}

/// Updates an order's status (admin/manager).
class UpdateOrderStatusUseCase
    implements Usecase<void, UpdateOrderStatusArgs> {
  final OrderRepository repository;

  UpdateOrderStatusUseCase(this.repository);

  @override
  Future<Either<Failure, void>> call(UpdateOrderStatusArgs params) =>
      repository.updateOrderStatus(params.orderId, params.request);
}

class UpdateOrderStatusArgs {
  final int orderId;
  final UpdateOrderStatusRequest request;

  const UpdateOrderStatusArgs({
    required this.orderId,
    required this.request,
  });
}

/// Assigns a driver to an order (admin/manager).
class AssignDriverUseCase implements Usecase<void, AssignDriverRequest> {
  final OrderRepository repository;

  AssignDriverUseCase(this.repository);

  @override
  Future<Either<Failure, void>> call(AssignDriverRequest params) =>
      repository.assignDriver(params);
}
