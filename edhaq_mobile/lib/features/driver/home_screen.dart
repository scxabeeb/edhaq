import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../../core/data/models/dashboard_model.dart';
import '../../core/data/models/order_models.dart';
import '../../core/di/injection.dart';
import '../../core/router/app_router.dart';
import '../../core/theme/app_theme.dart';
import '../../core/usecases/dashboard_usecases.dart';
import '../../core/usecases/usecase.dart';

class DriverHomeScreen extends StatefulWidget {
  const DriverHomeScreen({super.key});

  @override
  State<DriverHomeScreen> createState() => _DriverHomeScreenState();
}

class _DriverHomeScreenState extends State<DriverHomeScreen> {
  DriverDashboardModel? _dashboard;
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadDashboard();
  }

  Future<void> _loadDashboard() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });

    final result = await sl<GetDriverDashboardUseCase>()(const NoParams());

    if (!mounted) return;

    result.fold(
      (failure) {
        setState(() {
          _isLoading = false;
          _error = failure.message;
        });
      },
      (dashboard) {
        setState(() {
          _dashboard = dashboard;
          _isLoading = false;
        });
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      backgroundColor: theme.colorScheme.surface,
      appBar: AppBar(
        title: const Text('Driver Dashboard'),
        actions: [
          IconButton(
            icon: const Icon(Icons.notifications_outlined),
            onPressed: () => context.push(AppRoutes.notifications),
          ),
          IconButton(
            icon: const Icon(Icons.person_outline),
            onPressed: () => context.push(AppRoutes.profile),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: _loadDashboard,
        child: _isLoading
            ? const Center(child: CircularProgressIndicator())
            : _error != null
                ? _buildError(theme)
                : _buildDashboard(theme),
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
            onPressed: _loadDashboard,
            child: const Text('Retry'),
          ),
        ),
      ],
    );
  }

  Widget _buildDashboard(ThemeData theme) {
    final dashboard = _dashboard!;

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.all(16),
      children: [
        // Welcome card
        Card(
          child: Padding(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Welcome back,',
                  style: theme.textTheme.bodyMedium,
                ),
                const SizedBox(height: 4),
                Text(
                  dashboard.driverName,
                  style: theme.textTheme.titleLarge,
                ),
                const SizedBox(height: 16),
                Row(
                  children: [
                    Expanded(
                      child: _StatCard(
                        icon: Icons.local_shipping_outlined,
                        label: 'Active Jobs',
                        value: '${dashboard.activeAssignments}',
                        color: AppTheme.primaryColor,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: _StatCard(
                        icon: Icons.local_shipping,
                        label: 'Pickup',
                        value: '${dashboard.activePickupAssignments}',
                        color: AppTheme.accentColor,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: _StatCard(
                        icon: Icons.home_outlined,
                        label: 'Delivery',
                        value: '${dashboard.activeDeliveryAssignments}',
                        color: AppTheme.secondaryColor,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 12),
                Row(
                  children: [
                    Expanded(
                      child: _StatCard(
                        icon: Icons.account_balance_wallet_outlined,
                        label: 'Today',
                        value: '\$${dashboard.todayEarnings.toStringAsFixed(2)}',
                        color: AppTheme.secondaryColor,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: _StatCard(
                        icon: Icons.stars_outlined,
                        label: 'Rating',
                        value: dashboard.rating.toStringAsFixed(1),
                        color: AppTheme.accentColor,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: _StatCard(
                        icon: Icons.sync,
                        label: dashboard.status.displayName,
                        value: dashboard.isAvailable ? 'Avail' : 'Busy',
                        color: dashboard.isAvailable
                            ? AppTheme.secondaryColor
                            : AppTheme.errorColor,
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 24),

        // Quick actions
        Row(
          children: [
            Expanded(
              child: _ActionCard(
                icon: Icons.assignment_outlined,
                label: 'My Assignments',
                onTap: () => context.push(AppRoutes.driverAssignments),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: _ActionCard(
                icon: Icons.list_alt,
                label: 'New Jobs',
                onTap: () => context.push(AppRoutes.driverAssignments),
              ),
            ),
          ],
        ),
        const SizedBox(height: 24),

        // Current tasks
        if (dashboard.currentTasks.isNotEmpty) ...[
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text('Active Jobs', style: theme.textTheme.titleMedium),
            ],
          ),
          const SizedBox(height: 8),
          ...dashboard.currentTasks
              .map((order) => _DriverOrderCard(order: order)),
        ],
        const SizedBox(height: 24),

        // Unread notifications
        if (dashboard.unreadNotifications.isNotEmpty) ...[
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text('Notifications', style: theme.textTheme.titleMedium),
              TextButton(
                onPressed: () => context.push(AppRoutes.notifications),
                child: const Text('View All'),
              ),
            ],
          ),
          const SizedBox(height: 8),
          ...dashboard.unreadNotifications.take(3).map((notification) => Card(
                child: ListTile(
                  leading: CircleAvatar(
                    backgroundColor: AppTheme.primaryColor.withValues(alpha: 0.1),
                    child: Icon(
                      Icons.notifications,
                      color: AppTheme.primaryColor,
                    ),
                  ),
                  title: Text(
                    notification.title,
                    style: theme.textTheme.labelLarge,
                  ),
                  subtitle: Text(
                    notification.message,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
              )),
        ],
      ],
    );
  }
}

class _StatCard extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;
  final Color color;

  const _StatCard({
    required this.icon,
    required this.label,
    required this.value,
    required this.color,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        children: [
          Icon(icon, color: color, size: 28),
          const SizedBox(height: 8),
          Text(
            value,
            style: theme.textTheme.titleMedium?.copyWith(
              color: color,
              fontWeight: FontWeight.bold,
            ),
          ),
          const SizedBox(height: 2),
          Text(
            label,
            style: theme.textTheme.bodySmall?.copyWith(
              color: AppTheme.textSecondary,
            ),
            textAlign: TextAlign.center,
          ),
        ],
      ),
    );
  }
}

class _ActionCard extends StatelessWidget {
  final IconData icon;
  final String label;
  final VoidCallback onTap;

  const _ActionCard({
    required this.icon,
    required this.label,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(12),
      child: Container(
        padding: const EdgeInsets.symmetric(vertical: 16, horizontal: 8),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: const Color(0xFFE0E0E0)),
        ),
        child: Column(
          children: [
            Icon(icon, color: AppTheme.primaryColor, size: 28),
            const SizedBox(height: 8),
            Text(
              label,
              style: theme.textTheme.labelLarge,
              textAlign: TextAlign.center,
            ),
          ],
        ),
      ),
    );
  }
}

class _DriverOrderCard extends StatelessWidget {
  final OrderSummaryModel order;

  const _DriverOrderCard({required this.order});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: ListTile(
        leading: CircleAvatar(
          backgroundColor: AppTheme.primaryColor.withValues(alpha: 0.1),
          child: const Icon(
            Icons.local_laundry_service,
            color: AppTheme.primaryColor,
          ),
        ),
        title: Text(
          order.orderNumber,
          style: theme.textTheme.labelLarge,
        ),
        subtitle: Text(
          '${order.status.displayName} · ${_formatDate(order.createdAt)}',
          style: theme.textTheme.bodySmall,
        ),
        trailing: Text(
          '\$${order.totalAmount.toStringAsFixed(2)}',
          style: theme.textTheme.titleMedium?.copyWith(
            color: AppTheme.primaryColor,
            fontWeight: FontWeight.bold,
          ),
        ),
        onTap: () => context.push(AppRoutes.driverAssignments),
      ),
    );
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
