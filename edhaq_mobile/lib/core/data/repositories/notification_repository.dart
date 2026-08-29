import 'package:dartz/dartz.dart';
import 'package:dio/dio.dart';
import '../../data/errors/failures.dart';
import '../../data/models/notification_model.dart';
import '../../network/api_service.dart';
import '../../network/network_exception_mapper.dart';

abstract class NotificationRepository {
  Future<Either<Failure, List<NotificationModel>>> getNotifications({
    bool unreadOnly = true,
  });
  Future<Either<Failure, void>> markAsRead(int id);
}

class NotificationRepositoryImpl implements NotificationRepository {
  final ApiService apiService;

  NotificationRepositoryImpl(this.apiService);

  @override
  Future<Either<Failure, List<NotificationModel>>> getNotifications({
    bool unreadOnly = true,
  }) async {
    try {
      final result = await apiService.getNotifications(unreadOnly: unreadOnly);
      return Right(result);
    } on DioException catch (e) {
      return Left(mapDioExceptionToFailure(e));
    }
  }

  @override
  Future<Either<Failure, void>> markAsRead(int id) async {
    try {
      await apiService.markNotificationAsRead(id);
      return const Right(null);
    } on DioException catch (e) {
      return Left(mapDioExceptionToFailure(e));
    }
  }
}
