import 'package:dartz/dartz.dart';
import '../data/errors/failures.dart';
import '../data/models/address_models.dart';
import '../data/repositories/address_repository.dart';
import 'usecase.dart';

/// Fetches all addresses for the current customer.
class GetAddressesUseCase implements Usecase<List<AddressModel>, NoParams> {
  final AddressRepository repository;

  GetAddressesUseCase(this.repository);

  @override
  Future<Either<Failure, List<AddressModel>>> call(NoParams params) =>
      repository.getAddresses();
}

/// Creates a new address for the current customer.
class CreateAddressUseCase
    implements Usecase<AddressModel, CreateAddressRequest> {
  final AddressRepository repository;

  CreateAddressUseCase(this.repository);

  @override
  Future<Either<Failure, AddressModel>> call(CreateAddressRequest params) =>
      repository.createAddress(params);
}

/// Deletes an address by id.
class DeleteAddressUseCase implements Usecase<void, int> {
  final AddressRepository repository;

  DeleteAddressUseCase(this.repository);

  @override
  Future<Either<Failure, void>> call(int id) => repository.deleteAddress(id);
}