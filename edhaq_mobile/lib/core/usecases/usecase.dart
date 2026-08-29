import 'package:dartz/dartz.dart';
import '../data/errors/failures.dart';

/// Base class for all use cases.
///
/// Type parameters:
///   [T] – the return type of the use case (the "success" value).
///   [P] – the parameter type passed into [call].
///
/// Implementations return [Either<Failure, T>], which is a dartz
/// sum-type that lets callers handle errors in a functional way.
abstract class Usecase<T, P> {
  Future<Either<Failure, T>> call(P params);
}

/// Marker class for use cases that take no parameters.
class NoParams {
  const NoParams();
}