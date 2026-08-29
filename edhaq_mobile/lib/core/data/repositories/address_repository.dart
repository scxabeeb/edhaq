import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart';
import '../../data/errors/failures.dart';
import '../../data/models/address_models.dart';
import '../../network/api_service.dart';
import '../../network/network_exception_mapper.dart';

abstract class AddressRepository {
  Future<Either<Failure, List<AddressModel>>> getAddresses();
  Future<Either<Failure, AddressModel>> createAddress(
      CreateAddressRequest request);
  Future<Either<Failure, void>> deleteAddress(int id);
}

class AddressRepositoryImpl implements AddressRepository {
  final ApiService apiService;

  AddressRepositoryImpl(this.apiService);

  @override
  Future<Either<Failure, List<AddressModel>>> getAddresses() async {
    try {
      final result = await apiService.getAddresses();
      return Right(result);
    } on DioException catch (e) {
      return Left(mapDioExceptionToFailure(e));
    }
  }

  @override
  Future<Either<Failure, AddressModel>> createAddress(
      CreateAddressRequest request) async {
    try {
      final result = await apiService.createAddress(request);
      return Right(result);
    } on DioException catch (e) {
      return Left(mapDioExceptionToFailure(e));
    }
  }

  @override
  Future<Either<Failure, void>> deleteAddress(int id) async {
    try {
      await apiService.deleteAddress(id);
      return const Right(null);
    } on DioException catch (e) {
      return Left(mapDioExceptionToFailure(e));
    }
  }
}
