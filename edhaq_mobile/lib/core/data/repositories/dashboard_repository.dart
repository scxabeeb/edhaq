import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart';
import '../../data/errors/failures.dart';
import '../../data/models/dashboard_model.dart';
import '../../network/api_service.dart';
import '../../network/network_exception_mapper.dart';

abstract class DashboardRepository {
  Future<Either<Failure, CustomerDashboardModel>> getCustomerDashboard();
  Future<Either<Failure, DriverDashboardModel>> getDriverDashboard();
  Future<Either<Failure, AdminDashboardModel>> getAdminDashboard();
}

class DashboardRepositoryImpl implements DashboardRepository {
  final ApiService apiService;

  DashboardRepositoryImpl(this.apiService);

  @override
  Future<Either<Failure, CustomerDashboardModel>> getCustomerDashboard() async {
    try {
      final result = await apiService.getCustomerDashboard();
      return Right(result);
    } on DioException catch (e) {
      return Left(mapDioExceptionToFailure(e));
    }
  }

  @override
  Future<Either<Failure, DriverDashboardModel>> getDriverDashboard() async {
    try {
      final result = await apiService.getDriverDashboard();
      return Right(result);
    } on DioException catch (e) {
      return Left(mapDioExceptionToFailure(e));
    }
  }

  @override
  Future<Either<Failure, AdminDashboardModel>> getAdminDashboard() async {
    try {
      final result = await apiService.getAdminDashboard();
      return Right(result);
    } on DioException catch (e) {
      return Left(mapDioExceptionToFailure(e));
    }
  }
}
