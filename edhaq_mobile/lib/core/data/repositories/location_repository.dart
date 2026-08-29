import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart';
import '../../data/errors/failures.dart';
import '../../data/models/location_models.dart';
import '../../network/api_service.dart';
import '../../network/network_exception_mapper.dart';

abstract class LocationRepository {
  Future<Either<Failure, List<CityModel>>> getCities();
  Future<Either<Failure, List<VillageModel>>> getVillages(int cityId);
  Future<Either<Failure, List<SubVillageModel>>> getSubVillages(int villageId);
}

class LocationRepositoryImpl implements LocationRepository {
  final ApiService apiService;

  LocationRepositoryImpl(this.apiService);

  @override
  Future<Either<Failure, List<CityModel>>> getCities() async {
    try {
      final result = await apiService.getCities();
      return Right(result);
    } on DioException catch (e) {
      return Left(mapDioExceptionToFailure(e));
    }
  }

  @override
  Future<Either<Failure, List<VillageModel>>> getVillages(int cityId) async {
    try {
      final result = await apiService.getVillages(cityId);
      return Right(result);
    } on DioException catch (e) {
      return Left(mapDioExceptionToFailure(e));
    }
  }

  @override
  Future<Either<Failure, List<SubVillageModel>>> getSubVillages(
      int villageId) async {
    try {
      final result = await apiService.getSubVillages(villageId);
      return Right(result);
    } on DioException catch (e) {
      return Left(mapDioExceptionToFailure(e));
    }
  }
}
