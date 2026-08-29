import 'package:dartz/dartz.dart';
import '../data/errors/failures.dart';
import '../data/models/service_models.dart';
import '../data/repositories/service_repository.dart';
import 'usecase.dart';

/// Fetches all active service categories.
class GetServiceCategoriesUseCase
    implements Usecase<List<ServiceCategoryModel>, NoParams> {
  final ServiceRepository repository;

  GetServiceCategoriesUseCase(this.repository);

  @override
  Future<Either<Failure, List<ServiceCategoryModel>>> call(NoParams params) =>
      repository.getCategories();
}

/// Fetches services, optionally filtered by category.
class GetServicesUseCase
    implements Usecase<List<ServiceModel>, int?> {
  final ServiceRepository repository;

  GetServicesUseCase(this.repository);

  @override
  Future<Either<Failure, List<ServiceModel>>> call(int? categoryId) =>
      repository.getServices(categoryId: categoryId);
}