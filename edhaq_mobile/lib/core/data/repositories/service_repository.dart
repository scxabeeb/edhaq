import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart';
import '../../data/errors/failures.dart';
import '../../data/models/service_models.dart';
import '../../network/api_service.dart';
import '../../network/network_exception_mapper.dart';

abstract class ServiceRepository {
  Future<Either<Failure, List<ServiceCategoryModel>>> getCategories();
  Future<Either<Failure, List<ServiceModel>>> getServices({int? categoryId});
}

class ServiceRepositoryImpl implements ServiceRepository {
  final ApiService apiService;

  ServiceRepositoryImpl(this.apiService);

  @override
  Future<Either<Failure, List<ServiceCategoryModel>>> getCategories() async {
    try {
      final result = await apiService.getServiceCategories();
      return Right(result);
    } on DioException catch (e) {
      return Left(mapDioExceptionToFailure(e));
    }
  }

  @override
  Future<Either<Failure, List<ServiceModel>>> getServices({int? categoryId}) async {
    try {
      final result = await apiService.getServices(categoryId: categoryId);
      return Right(result);
    } on DioException catch (e) {
      return Left(mapDioExceptionToFailure(e));
    }
  }
}
