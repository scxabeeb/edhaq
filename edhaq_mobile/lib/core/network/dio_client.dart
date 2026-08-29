import 'package:dio/dio.dart';
import '../constants/app_constants.dart';
import '../data/local/secure_storage_service.dart';

class DioClient {
  final Dio _dio;
  final SecureStorageService _secureStorage;

  DioClient(this._secureStorage)
      : _dio = Dio(BaseOptions(
          baseUrl: AppConstants.baseUrl,
          connectTimeout: AppConstants.connectTimeout,
          receiveTimeout: AppConstants.receiveTimeout,
          headers: {'Content-Type': 'application/json'},
        )) {
    _dio.interceptors.add(_authInterceptor());
    _dio.interceptors.add(LogInterceptor(requestBody: true, responseBody: true));
  }

  Dio get dio => _dio;

  Interceptor _authInterceptor() {
    return InterceptorsWrapper(
      onRequest: (options, handler) async {
        final token = await _secureStorage.read(AppConstants.authTokenKey);
        if (token != null && token.isNotEmpty) {
          options.headers['Authorization'] = 'Bearer $token';
        }
        return handler.next(options);
      },
      onError: (error, handler) async {
        final statusCode = error.response?.statusCode;
        final path = error.requestOptions.path;
        // 401: token expired/invalid.
        // 404 on /auth/me: token references a user that no longer exists
        // (e.g. database was re-seeded). Clear the stale session either way.
        if (statusCode == 401 ||
            (statusCode == 404 && path.contains('/auth/me'))) {
          await _secureStorage.deleteAll();
        }
        return handler.next(error);
      },
    );
  }
}
