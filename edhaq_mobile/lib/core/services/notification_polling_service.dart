import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';

import '../di/injection.dart';
import '../router/app_router.dart';
import '../usecases/notification_usecases.dart';

/// Polls the backend for unread notifications and surfaces them as
/// system (tray) notifications. Works for both drivers and customers:
///   - Drivers get "New pickup/delivery task" alerts when staff assign work.
///   - Customers get order/payment/delivery alerts.
/// Also exposes an [unreadCount] notifier for the app-bar bell badge.
class NotificationPollingService {
  NotificationPollingService(this._getNotifications);

  final GetNotificationsUseCase _getNotifications;

  final FlutterLocalNotificationsPlugin _plugin =
      FlutterLocalNotificationsPlugin();

  Timer? _timer;
  bool _initialized = false;
  final Set<int> _shownIds = {};
  int _consecutiveFailures = 0;

  /// Unread notification count for UI badges (app-bar bell).
  final ValueNotifier<int> unreadCount = ValueNotifier<int>(0);

  static const Duration _interval = Duration(seconds: 30);
  static const int _maxFailuresBeforeBackoff = 3;

  Future<void> initialize() async {
    if (_initialized) return;
    const androidInit = AndroidInitializationSettings('@mipmap/ic_launcher');
    const iosInit = DarwinInitializationSettings(
      requestAlertPermission: true,
      requestBadgePermission: true,
      requestSoundPermission: true,
    );
    await _plugin.initialize(
      const InitializationSettings(android: androidInit, iOS: iosInit),
      onDidReceiveNotificationResponse: _onTap,
    );
    // Android 13+ runtime permission.
    await _plugin
        .resolvePlatformSpecificImplementation<
            AndroidFlutterLocalNotificationsPlugin>()
        ?.requestNotificationsPermission();
    _initialized = true;
  }

  void _onTap(NotificationResponse response) {
    // Payload carries the route to open (e.g. /Driver/Assignments).
    final payload = response.payload;
    if (payload != null && payload.isNotEmpty) {
      appRouter.push(payload);
    }
  }

  /// Starts polling. Safe to call multiple times.
  Future<void> start() async {
    await initialize();
    _timer?.cancel();
    _consecutiveFailures = 0;
    await _poll(baseline: true);
    _timer = Timer.periodic(_interval, (_) => _poll());
  }

  void stop() {
    _timer?.cancel();
    _timer = null;
    unreadCount.value = 0;
  }

  Future<void> _poll({bool baseline = false}) async {
    final result = await _getNotifications(
      const GetNotificationsParams(unreadOnly: true),
    );

    result.fold(
      (_) {
        // Unauthenticated (logged out) or network error — back off so we
        // don't spam the API when there is no session.
        _consecutiveFailures++;
        if (_consecutiveFailures >= _maxFailuresBeforeBackoff) {
          _timer?.cancel();
          _timer = null;
          unreadCount.value = 0;
        }
      },
      (notifications) {
        _consecutiveFailures = 0;
        unreadCount.value = notifications.length;

        // On the very first successful poll after login, only seed the
        // baseline (don't blast the user with every historical item).
        for (final n in notifications) {
          if (baseline && _shownIds.isEmpty) {
            _shownIds.add(n.id);
            continue;
          }
          if (_shownIds.contains(n.id)) continue;
          _shownIds.add(n.id);
          _show(n.id, n.title, n.message, n.actionUrl);
        }
      },
    );
  }

  Future<void> _show(int id, String title, String body, String? route) async {
    const androidDetails = AndroidNotificationDetails(
      'edhaq_tasks',
      'Tasks & Updates',
      channelDescription:
          'New task assignments and order/payment notifications',
      importance: Importance.high,
      priority: Priority.high,
      styleInformation: BigTextStyleInformation(''),
    );
    const iosDetails = DarwinNotificationDetails(
      presentAlert: true,
      presentSound: true,
      presentBadge: true,
    );
    await _plugin.show(
      id,
      title.isEmpty ? 'eDhaq Laundry' : title,
      body,
      const NotificationDetails(
        android: androidDetails,
        iOS: iosDetails,
      ),
      payload: (route == null || route.isEmpty) ? null : route,
    );
  }
}

/// Convenience accessor so screens can reach the global instance.
NotificationPollingService get notificationPolling =>
    sl<NotificationPollingService>();