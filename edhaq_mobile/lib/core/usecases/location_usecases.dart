import 'package:dartz/dartz.dart';
import '../data/errors/failures.dart';
import '../data/models/location_models.dart';
import '../data/repositories/location_repository.dart';
import 'usecase.dart';

/// Fetches all active cities.
class GetCitiesUseCase implements Usecase<List<CityModel>, NoParams> {
  final LocationRepository repository;

  GetCitiesUseCase(this.repository);

  @override
  Future<Either<Failure, List<CityModel>>> call(NoParams params) =>
      repository.getCities();
}

/// Fetches villages for a given city.
class GetVillagesUseCase implements Usecase<List<VillageModel>, int> {
  final LocationRepository repository;

  GetVillagesUseCase(this.repository);

  @override
  Future<Either<Failure, List<VillageModel>>> call(int cityId) =>
      repository.getVillages(cityId);
}

/// Fetches sub-villages for a given village.
class GetSubVillagesUseCase
    implements Usecase<List<SubVillageModel>, int> {
  final LocationRepository repository;

  GetSubVillagesUseCase(this.repository);

  @override
  Future<Either<Failure, List<SubVillageModel>>> call(int villageId) =>
      repository.getSubVillages(villageId);
}