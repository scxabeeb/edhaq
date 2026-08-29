import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart';
import '../../data/errors/failures.dart';
import '../../data/models/auth_request_models.dart';
import '../../data/models/user_model.dart';
import '../../network/api_service.dart';
import '../../network/network_exception_mapper.dart';

abstract class AuthRepository {
  Future<Either<Failure, LoginResponse>> login(LoginRequest request);
  Future<Either<Failure, LoginResponse>> register(RegisterRequest request);
  Future<Either<Failure, AppUser>> getCurrentUser();
  Future<Either<Failure, void>> logout();
}

class AuthRepositoryImpl implements AuthRepository {
  final ApiService apiService;

  AuthRepositoryImpl(this.apiService);

  @override
  Future<Either<Failure, LoginResponse>> login(LoginRequest request) async {
    try {
      final result = await apiService.login(request);
      return Right(result);
    } on DioException catch (e) {
      return Left(mapDioExceptionToFailure(e));
    }
  }

  @override
  Future<Either<Failure, LoginResponse>> register(RegisterRequest request) async {
    try {
      final result = await apiService.register(request);
      return Right(result);
    } on DioException catch (e) {
      return Left(mapDioExceptionToFailure(e));
    }
  }

  @override
  Future<Either<Failure, AppUser>> getCurrentUser() async {
    try {
      final result = await apiService.getCurrentUser();
      return Right(result);
    } on DioException catch (e) {
      return Left(mapDioExceptionToFailure(e));
    }
  }

  @override
  Future<Either<Failure, void>> logout() async {
    try {
      await apiService.logout();
      return const Right(null);
    } on DioException catch (e) {
      return Left(mapDioExceptionToFailure(e));
    }
  }
}
