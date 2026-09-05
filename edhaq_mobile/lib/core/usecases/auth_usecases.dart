import 'package:dartz/dartz.dart';
import '../data/errors/failures.dart';
import '../data/models/auth_request_models.dart';
import '../data/models/user_model.dart';
import '../data/repositories/auth_repository.dart';
import 'usecase.dart';


/// Logs a user in with email + password.
class LoginUseCase implements Usecase<LoginResponse, LoginRequest> {
  final AuthRepository repository;

  LoginUseCase(this.repository);

  @override
  Future<Either<Failure, LoginResponse>> call(LoginRequest params) =>
      repository.login(params);
}

/// Registers a new customer account.
class RegisterUseCase implements Usecase<LoginResponse, RegisterRequest> {
  final AuthRepository repository;

  RegisterUseCase(this.repository);

  @override
  Future<Either<Failure, LoginResponse>> call(RegisterRequest params) =>
      repository.register(params);
}

/// Fetches the currently authenticated user's profile.
class GetCurrentUserUseCase implements Usecase<AppUser, NoParams> {
  final AuthRepository repository;

  GetCurrentUserUseCase(this.repository);

  @override
  Future<Either<Failure, AppUser>> call(NoParams params) =>
      repository.getCurrentUser();
}

/// Logs the current user out.
class LogoutUseCase implements Usecase<void, NoParams> {
  final AuthRepository repository;

  LogoutUseCase(this.repository);

  @override
  Future<Either<Failure, void>> call(NoParams params) => repository.logout();
}

/// Changes the current user's password.
class ChangePasswordUseCase implements Usecase<void, ChangePasswordRequest> {
  final AuthRepository repository;

  ChangePasswordUseCase(this.repository);

  @override
  Future<Either<Failure, void>> call(ChangePasswordRequest params) =>
      repository.changePassword(params);
}