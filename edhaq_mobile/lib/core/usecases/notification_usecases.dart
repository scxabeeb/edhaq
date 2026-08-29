import 'package:dartz/dartz.dart';
import '../data/errors/failures.dart';
import '../data/models/notification_model.dart';
import '../data/repositories/notification_repository.dart';
import 'usecase.dart';

/// Parameters for fetching notifications.
class GetNotificationsParams {
  final bool unreadOnly;

  const GetNotificationsParams({this.unreadOnly = true});
}

/// Fetches notifications for the current user.
class GetNotificationsUseCase
    implements Usecase<List<NotificationModel>, GetNotificationsParams> {
  final NotificationRepository repository;

  GetNotificationsUseCase(this.repository);

  @override
  Future<Either<Failure, List<NotificationModel>>> call(
          GetNotificationsParams params) =>
      repository.getNotifications(unreadOnly: params.unreadOnly);
}

/// Marks a single notification as read.
class MarkNotificationAsReadUseCase implements Usecase<void, int> {
  final NotificationRepository repository;

  MarkNotificationAsReadUseCase(this.repository);

  @override
  Future<Either<Failure, void>> call(int id) => repository.markAsRead(id);
}