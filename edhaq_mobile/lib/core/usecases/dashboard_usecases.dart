import 'package:dartz/dartz.dart';
import '../data/errors/failures.dart';
import '../data/models/dashboard_model.dart';
import '../data/repositories/dashboard_repository.dart';
import 'usecase.dart';

/// Fetches the customer dashboard summary.
class GetCustomerDashboardUseCase
    implements Usecase<CustomerDashboardModel, NoParams> {
  final DashboardRepository repository;

  GetCustomerDashboardUseCase(this.repository);

  @override
  Future<Either<Failure, CustomerDashboardModel>> call(NoParams params) =>
      repository.getCustomerDashboard();
}

/// Fetches the driver dashboard summary.
class GetDriverDashboardUseCase
    implements Usecase<DriverDashboardModel, NoParams> {
  final DashboardRepository repository;

  GetDriverDashboardUseCase(this.repository);

  @override
  Future<Either<Failure, DriverDashboardModel>> call(NoParams params) =>
      repository.getDriverDashboard();
}

/// Fetches the admin dashboard summary.
class GetAdminDashboardUseCase
    implements Usecase<AdminDashboardModel, NoParams> {
  final DashboardRepository repository;

  GetAdminDashboardUseCase(this.repository);

  @override
  Future<Either<Failure, AdminDashboardModel>> call(NoParams params) =>
      repository.getAdminDashboard();
}
