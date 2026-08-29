import 'package:get_it/get_it.dart';
import '../data/local/secure_storage_service.dart';
import '../data/repositories/address_repository.dart';
import '../data/repositories/auth_repository.dart';
import '../data/repositories/dashboard_repository.dart';
import '../data/repositories/location_repository.dart';
import '../data/repositories/notification_repository.dart';
import '../data/repositories/order_repository.dart';
import '../data/repositories/service_repository.dart';
import '../network/api_service.dart';
import '../network/dio_client.dart';
import '../usecases/address_usecases.dart';
import '../usecases/auth_usecases.dart';
import '../usecases/dashboard_usecases.dart';
import '../usecases/location_usecases.dart';
import '../usecases/notification_usecases.dart';
import '../usecases/order_usecases.dart';
import '../usecases/service_usecases.dart';

/// Global service locator.
final GetIt sl = GetIt.instance;

/// Registers all dependencies for the app.
Future<void> initDependencies() async {
  // ── Core ──────────────────────────────────────────────────────────────
  sl.registerLazySingleton<SecureStorageService>(
    () => SecureStorageService(),
  );

  sl.registerLazySingleton<DioClient>(
    () => DioClient(sl<SecureStorageService>()),
  );

  sl.registerLazySingleton<ApiService>(
    () => ApiService(sl<DioClient>().dio),
  );

  // ── Repositories ──────────────────────────────────────────────────────
  sl.registerLazySingleton<AuthRepository>(
    () => AuthRepositoryImpl(sl<ApiService>()),
  );
  sl.registerLazySingleton<LocationRepository>(
    () => LocationRepositoryImpl(sl<ApiService>()),
  );
  sl.registerLazySingleton<ServiceRepository>(
    () => ServiceRepositoryImpl(sl<ApiService>()),
  );
  sl.registerLazySingleton<AddressRepository>(
    () => AddressRepositoryImpl(sl<ApiService>()),
  );
  sl.registerLazySingleton<OrderRepository>(
    () => OrderRepositoryImpl(sl<ApiService>()),
  );
  sl.registerLazySingleton<NotificationRepository>(
    () => NotificationRepositoryImpl(sl<ApiService>()),
  );
  sl.registerLazySingleton<DashboardRepository>(
    () => DashboardRepositoryImpl(sl<ApiService>()),
  );

  // ── Use cases ─────────────────────────────────────────────────────────
  // Auth
  sl.registerLazySingleton<LoginUseCase>(
    () => LoginUseCase(sl<AuthRepository>()),
  );
  sl.registerLazySingleton<RegisterUseCase>(
    () => RegisterUseCase(sl<AuthRepository>()),
  );
  sl.registerLazySingleton<GetCurrentUserUseCase>(
    () => GetCurrentUserUseCase(sl<AuthRepository>()),
  );
  sl.registerLazySingleton<LogoutUseCase>(
    () => LogoutUseCase(sl<AuthRepository>()),
  );

  // Locations
  sl.registerLazySingleton<GetCitiesUseCase>(
    () => GetCitiesUseCase(sl<LocationRepository>()),
  );
  sl.registerLazySingleton<GetVillagesUseCase>(
    () => GetVillagesUseCase(sl<LocationRepository>()),
  );
  sl.registerLazySingleton<GetSubVillagesUseCase>(
    () => GetSubVillagesUseCase(sl<LocationRepository>()),
  );

  // Services
  sl.registerLazySingleton<GetServiceCategoriesUseCase>(
    () => GetServiceCategoriesUseCase(sl<ServiceRepository>()),
  );
  sl.registerLazySingleton<GetServicesUseCase>(
    () => GetServicesUseCase(sl<ServiceRepository>()),
  );

  // Addresses
  sl.registerLazySingleton<GetAddressesUseCase>(
    () => GetAddressesUseCase(sl<AddressRepository>()),
  );
  sl.registerLazySingleton<CreateAddressUseCase>(
    () => CreateAddressUseCase(sl<AddressRepository>()),
  );
  sl.registerLazySingleton<DeleteAddressUseCase>(
    () => DeleteAddressUseCase(sl<AddressRepository>()),
  );

  // Orders (customer)
  sl.registerLazySingleton<GetOrdersUseCase>(
    () => GetOrdersUseCase(sl<OrderRepository>()),
  );
  sl.registerLazySingleton<GetOrderUseCase>(
    () => GetOrderUseCase(sl<OrderRepository>()),
  );
  sl.registerLazySingleton<CreateOrderUseCase>(
    () => CreateOrderUseCase(sl<OrderRepository>()),
  );
  sl.registerLazySingleton<ConfirmDeliveryUseCase>(
    () => ConfirmDeliveryUseCase(sl<OrderRepository>()),
  );
  sl.registerLazySingleton<PayOrderUseCase>(
    () => PayOrderUseCase(sl<OrderRepository>()),
  );

  // Orders (driver)
  sl.registerLazySingleton<GetDriverAssignmentsUseCase>(
    () => GetDriverAssignmentsUseCase(sl<OrderRepository>()),
  );
  sl.registerLazySingleton<AcceptDriverAssignmentUseCase>(
    () => AcceptDriverAssignmentUseCase(sl<OrderRepository>()),
  );
  sl.registerLazySingleton<NotifyOnTheWayUseCase>(
    () => NotifyOnTheWayUseCase(sl<OrderRepository>()),
  );
  sl.registerLazySingleton<NotifyAtGateUseCase>(
    () => NotifyAtGateUseCase(sl<OrderRepository>()),
  );
  sl.registerLazySingleton<CompleteDriverAssignmentUseCase>(
    () => CompleteDriverAssignmentUseCase(sl<OrderRepository>()),
  );
  sl.registerLazySingleton<CollectPaymentUseCase>(
    () => CollectPaymentUseCase(sl<OrderRepository>()),
  );

  // Orders (admin)
  sl.registerLazySingleton<GetAdminOrdersUseCase>(
    () => GetAdminOrdersUseCase(sl<OrderRepository>()),
  );
  sl.registerLazySingleton<UpdateOrderStatusUseCase>(
    () => UpdateOrderStatusUseCase(sl<OrderRepository>()),
  );
  sl.registerLazySingleton<AssignDriverUseCase>(
    () => AssignDriverUseCase(sl<OrderRepository>()),
  );

  // Notifications
  sl.registerLazySingleton<GetNotificationsUseCase>(
    () => GetNotificationsUseCase(sl<NotificationRepository>()),
  );
  sl.registerLazySingleton<MarkNotificationAsReadUseCase>(
    () => MarkNotificationAsReadUseCase(sl<NotificationRepository>()),
  );

  // Dashboard
  sl.registerLazySingleton<GetCustomerDashboardUseCase>(
    () => GetCustomerDashboardUseCase(sl<DashboardRepository>()),
  );
  sl.registerLazySingleton<GetDriverDashboardUseCase>(
    () => GetDriverDashboardUseCase(sl<DashboardRepository>()),
  );
  sl.registerLazySingleton<GetAdminDashboardUseCase>(
    () => GetAdminDashboardUseCase(sl<DashboardRepository>()),
  );
}
