import 'package:flutter/material.dart';
import '../../features/customer/notifications_screen.dart';
import '../di/injection.dart';
import '../services/notification_polling_service.dart';

/// App-bar bell icon with a live unread-count badge, driven by the
/// global [NotificationPollingService]. Tapping opens the notifications
/// screen. Used on both the driver and customer home screens.
class NotificationBell extends StatelessWidget {
  const NotificationBell({super.key, this.iconSize = 24});

  final double iconSize;

  @override
  Widget build(BuildContext context) {
    return IconButton(
      iconSize: iconSize,
      icon: Stack(
        clipBehavior: Clip.none,
        children: [
          const Icon(Icons.notifications_outlined),
          ValueListenableBuilder<int>(
            valueListenable: sl<NotificationPollingService>().unreadCount,
            builder: (context, count, _) {
              if (count <= 0) return const SizedBox.shrink();
              return Positioned(
                right: -6,
                top: -4,
                child: Container(
                  padding: const EdgeInsets.all(4),
                  decoration: BoxDecoration(
                    color: Theme.of(context).colorScheme.error,
                    shape: BoxShape.circle,
                  ),
                  constraints: const BoxConstraints(minWidth: 16, minHeight: 16),
                  child: Text(
                    count > 99 ? '99+' : '$count',
                    textAlign: TextAlign.center,
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 9,
                      fontWeight: FontWeight.bold,
                      height: 1,
                    ),
                  ),
                ),
              );
            },
          ),
        ],
      ),
      onPressed: () {
        // Clear the badge when the user opens the list.
        sl<NotificationPollingService>().unreadCount.value = 0;
        Navigator.of(context).push(
          MaterialPageRoute(builder: (_) => const NotificationsScreen()),
        );
      },
    );
  }
}