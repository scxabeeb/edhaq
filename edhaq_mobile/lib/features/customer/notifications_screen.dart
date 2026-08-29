import 'package:flutter/material.dart';
import 'package:fluttertoast/fluttertoast.dart';
import 'package:go_router/go_router.dart';
import '../../core/data/models/notification_model.dart';
import '../../core/di/injection.dart';
import '../../core/theme/app_theme.dart';
import '../../core/usecases/notification_usecases.dart';

class NotificationsScreen extends StatefulWidget {
  const NotificationsScreen({super.key});

  @override
  State<NotificationsScreen> createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends State<NotificationsScreen> {
  List<NotificationModel> _notifications = [];
  bool _isLoading = true;
  String? _error;
  bool _showUnreadOnly = true;

  @override
  void initState() {
    super.initState();
    _loadNotifications();
  }

  Future<void> _loadNotifications() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });

    final result = await sl<GetNotificationsUseCase>()(
      GetNotificationsParams(unreadOnly: _showUnreadOnly),
    );

    if (!mounted) return;

    result.fold(
      (failure) {
        setState(() {
          _isLoading = false;
          _error = failure.message;
        });
      },
      (notifications) {
        setState(() {
          _notifications = notifications;
          _isLoading = false;
        });
      },
    );
  }

  Future<void> _markAsRead(int id) async {
    final result = await sl<MarkNotificationAsReadUseCase>()(id);

    if (!mounted) return;

    result.fold(
      (failure) {
        Fluttertoast.showToast(
          msg: failure.message,
          toastLength: Toast.LENGTH_LONG,
          gravity: ToastGravity.BOTTOM,
          backgroundColor: Colors.red.shade700,
          textColor: Colors.white,
        );
      },
      (_) {
        _loadNotifications();
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      backgroundColor: theme.colorScheme.surface,
      appBar: AppBar(
        title: const Text('Notifications'),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.pop(),
        ),
        actions: [
          IconButton(
            icon: Icon(
              _showUnreadOnly ? Icons.mark_email_read : Icons.mark_email_unread,
            ),
            onPressed: () {
              setState(() => _showUnreadOnly = !_showUnreadOnly);
              _loadNotifications();
            },
            tooltip: _showUnreadOnly ? 'Show all' : 'Show unread only',
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: _loadNotifications,
        child: _isLoading
            ? const Center(child: CircularProgressIndicator())
            : _error != null
                ? _buildError(theme)
                : _notifications.isEmpty
                    ? _buildEmpty(theme)
                    : _buildNotificationsList(theme),
      ),
    );
  }

  Widget _buildError(ThemeData theme) {
    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      children: [
        const SizedBox(height: 120),
        Icon(Icons.error_outline, size: 64, color: theme.colorScheme.error),
        const SizedBox(height: 16),
        Text(
          _error ?? 'Something went wrong',
          textAlign: TextAlign.center,
          style: theme.textTheme.bodyLarge,
        ),
        const SizedBox(height: 24),
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 48),
          child: ElevatedButton(
            onPressed: _loadNotifications,
            child: const Text('Retry'),
          ),
        ),
      ],
    );
  }

  Widget _buildEmpty(ThemeData theme) {
    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      children: [
        const SizedBox(height: 120),
        Icon(
          Icons.notifications_none,
          size: 64,
          color: theme.colorScheme.outline,
        ),
        const SizedBox(height: 16),
        Text(
          _showUnreadOnly ? 'No unread notifications' : 'No notifications',
          textAlign: TextAlign.center,
          style: theme.textTheme.titleLarge,
        ),
      ],
    );
  }

  Widget _buildNotificationsList(ThemeData theme) {
    return ListView.builder(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.all(16),
      itemCount: _notifications.length,
      itemBuilder: (context, index) {
        final notification = _notifications[index];
        return Card(
          margin: const EdgeInsets.only(bottom: 12),
          child: ListTile(
            leading: CircleAvatar(
              backgroundColor: notification.isRead
                  ? AppTheme.textSecondary.withValues(alpha: 0.1)
                  : AppTheme.primaryColor.withValues(alpha: 0.1),
              child: Icon(
                _notificationIcon(notification.type),
                color: notification.isRead
                    ? AppTheme.textSecondary
                    : AppTheme.primaryColor,
              ),
            ),
            title: Text(
              notification.title,
              style: theme.textTheme.labelLarge?.copyWith(
                fontWeight:
                    notification.isRead ? FontWeight.normal : FontWeight.bold,
              ),
            ),
            subtitle: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const SizedBox(height: 4),
                Text(
                  notification.message,
                  style: theme.textTheme.bodySmall,
                ),
                const SizedBox(height: 4),
                Text(
                  _formatDate(notification.createdAt),
                  style: theme.textTheme.bodySmall?.copyWith(
                    color: AppTheme.textSecondary,
                  ),
                ),
              ],
            ),
            trailing: notification.isRead
                ? null
                : IconButton(
                    icon: const Icon(Icons.check_circle_outline),
                    onPressed: () => _markAsRead(notification.id),
                    tooltip: 'Mark as read',
                  ),
          ),
        );
      },
    );
  }

  IconData _notificationIcon(NotificationType type) {
    switch (type) {
      case NotificationType.orderCreated:
        return Icons.add_shopping_cart;
      case NotificationType.pickupScheduled:
        return Icons.schedule;
      case NotificationType.driverAssigned:
        return Icons.person;
      case NotificationType.laundryStarted:
        return Icons.local_laundry_service;
      case NotificationType.laundryReady:
        return Icons.check_circle;
      case NotificationType.outForDelivery:
        return Icons.local_shipping;
      case NotificationType.delivered:
        return Icons.done_all;
      case NotificationType.paymentConfirmed:
        return Icons.payment;
      case NotificationType.general:
      default:
        return Icons.notifications;
    }
  }

  String _formatDate(DateTime date) {
    final now = DateTime.now();
    final diff = now.difference(date);
    if (diff.inDays > 0) {
      return '${diff.inDays}d ago';
    }
    if (diff.inHours > 0) {
      return '${diff.inHours}h ago';
    }
    if (diff.inMinutes > 0) {
      return '${diff.inMinutes}m ago';
    }
    return 'Just now';
  }
}