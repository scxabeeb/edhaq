import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../../core/data/models/dashboard_model.dart';
import '../../core/data/models/order_models.dart';
import '../../core/di/injection.dart';
import '../../core/router/app_router.dart';
import '../../core/theme/app_theme.dart';
import '../../core/usecases/dashboard_usecases.dart';
import '../../core/usecases/usecase.dart';
import 'orders_screen.dart';

class AdminHomeScreen extends StatefulWidget {
  const AdminHomeScreen({super.key});

  @override
  State<AdminHomeScreen> createState() => _AdminHomeScreenState();
}

class _AdminHomeScreenState extends State<AdminHomeScreen> {
  AdminDashboardModel? _dashboard;
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

    final result = await sl<GetAdminDashboardUseCase>()(const NoParams());

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
        title: const Text('Admin Dashboard'),
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
        // Welcome header
        _buildWelcomeHeader(theme, dashboard),
        const SizedBox(height: 20),

        // Key metrics grid
        _buildMetricsGrid(theme, dashboard),
        const SizedBox(height: 24),

        // Order status breakdown
        if (dashboard.statusCounts.isNotEmpty) ...[
          _buildStatusBreakdown(theme, dashboard),
          const SizedBox(height: 24),
        ],

        // Quick actions
        _buildQuickActions(theme),
        const SizedBox(height: 24),

        // Recent orders
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text('Recent Orders', style: theme.textTheme.titleMedium),
            TextButton(
              onPressed: () => context.push(AppRoutes.adminOrders),
              child: const Text('View All'),
            ),
          ],
        ),
        const SizedBox(height: 8),

        if (dashboard.recentOrders.isEmpty)
          _buildEmptyOrders(theme)
        else
          ...dashboard.recentOrders.map((order) => _OrderStatCard(order: order)),
      ],
    );
  }

  Widget _buildWelcomeHeader(ThemeData theme, AdminDashboardModel dashboard) {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        gradient: LinearGradient(
          colors: [
            AppTheme.primaryColor,
            AppTheme.primaryColor.withValues(alpha: 0.8),
          ],
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
        borderRadius: BorderRadius.circular(16),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'Welcome back,',
            style: theme.textTheme.bodyMedium?.copyWith(
              color: Colors.white.withValues(alpha: 0.9),
            ),
          ),
          const SizedBox(height: 4),
          Text(
            dashboard.adminName,
            style: theme.textTheme.titleLarge?.copyWith(
              color: Colors.white,
              fontWeight: FontWeight.bold,
            ),
          ),
          const SizedBox(height: 12),
          Text(
            'Here\'s what\'s happening with your laundry business today.',
            style: theme.textTheme.bodySmall?.copyWith(
              color: Colors.white.withValues(alpha: 0.85),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildMetricsGrid(ThemeData theme, AdminDashboardModel dashboard) {
    final metrics = [
      _MetricData(
        icon: Icons.shopping_bag_outlined,
        label: 'Total Orders',
        value: '${dashboard.totalOrders}',
        color: AppTheme.primaryColor,
        onTap: () => context.push(AppRoutes.adminOrders),
      ),
      _MetricData(
        icon: Icons.pending_actions_outlined,
        label: 'Active',
        value: '${dashboard.activeOrders}',
        color: AppTheme.accentColor,
        onTap: () => context.push(
          AppRoutes.adminOrders,
          extra: const AdminOrdersFilter(activeOnly: true),
        ),
      ),
      _MetricData(
        icon: Icons.check_circle_outline,
        label: 'Completed',
        value: '${dashboard.completedOrders}',
        color: AppTheme.secondaryColor,
        onTap: () => context.push(
          AppRoutes.adminOrders,
          extra: const AdminOrdersFilter(status: OrderStatus.completed),
        ),
      ),
      _MetricData(
        icon: Icons.attach_money,
        label: 'Revenue',
        value: '\$${dashboard.totalRevenue.toStringAsFixed(2)}',
        color: AppTheme.secondaryColor,
      ),
      _MetricData(
        icon: Icons.people_outline,
        label: 'Customers',
        value: '${dashboard.totalCustomers}',
        color: AppTheme.primaryColor,
      ),
      _MetricData(
        icon: Icons.local_shipping_outlined,
        label: 'Drivers',
        value: '${dashboard.totalDrivers}',
        color: AppTheme.accentColor,
      ),
    ];

    return GridView.builder(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      gridDelegate: const SliverGridDelegateWithMaxCrossAxisExtent(
        maxCrossAxisExtent: 180,
        mainAxisSpacing: 12,
        crossAxisSpacing: 12,
        childAspectRatio: 1.1,
      ),
      itemCount: metrics.length,
      itemBuilder: (context, index) => _MetricCard(data: metrics[index]),
    );
  }

  Widget _buildStatusBreakdown(ThemeData theme, AdminDashboardModel dashboard) {
    final statusColors = <String, Color>{
      'OrderPlaced': AppTheme.primaryColor,
      'DriverAssigned': AppTheme.accentColor,
      'DriverOnTheWay': AppTheme.accentColor,
      'ClothesPickedUp': AppTheme.accentColor,
      'LaundryReceived': AppTheme.accentColor,
      'Sorting': AppTheme.accentColor,
      'Washing': AppTheme.accentColor,
      'Drying': AppTheme.accentColor,
      'Ironing': AppTheme.accentColor,
      'Folding': AppTheme.accentColor,
      'Packaging': AppTheme.accentColor,
      'ReadyForDelivery': AppTheme.accentColor,
      'OutForDelivery': AppTheme.accentColor,
      'Delivered': AppTheme.secondaryColor,
      'Completed': AppTheme.secondaryColor,
      'Cancelled': AppTheme.errorColor,
    };

    final entries = dashboard.statusCounts.entries
        .where((e) => e.value > 0)
        .toList()
      ..sort((a, b) => b.value.compareTo(a.value));

    if (entries.isEmpty) return const SizedBox.shrink();

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Order Status', style: theme.textTheme.titleMedium),
            const SizedBox(height: 12),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: entries.map((entry) {
                final color = statusColors[entry.key] ?? AppTheme.primaryColor;
                return Chip(
                  label: Text('${entry.key} (${entry.value})'),
                  backgroundColor: color.withValues(alpha: 0.1),
                  labelStyle: TextStyle(
                    color: color,
                    fontWeight: FontWeight.w600,
                    fontSize: 12,
                  ),
                  side: BorderSide(color: color.withValues(alpha: 0.3)),
                );
              }).toList(),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildQuickActions(ThemeData theme) {
    final actions = [
      _ActionData(
        icon: Icons.list_alt,
        label: 'All Orders',
        onTap: () => context.push(AppRoutes.adminOrders),
      ),
      _ActionData(
        icon: Icons.local_shipping,
        label: 'Drivers',
        onTap: () => context.push(AppRoutes.adminOrders),
      ),
      _ActionData(
        icon: Icons.category_outlined,
        label: 'Services',
        onTap: () => context.push(AppRoutes.adminOrders),
      ),
      _ActionData(
        icon: Icons.people_outline,
        label: 'Users',
        onTap: () => context.push(AppRoutes.adminOrders),
      ),
    ];

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('Quick Actions', style: theme.textTheme.titleMedium),
        const SizedBox(height: 12),
        GridView.builder(
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          gridDelegate: const SliverGridDelegateWithMaxCrossAxisExtent(
            maxCrossAxisExtent: 180,
            mainAxisSpacing: 12,
            crossAxisSpacing: 12,
            childAspectRatio: 1.3,
          ),
          itemCount: actions.length,
          itemBuilder: (context, index) => _ActionCard(data: actions[index]),
        ),
      ],
    );
  }

  Widget _buildEmptyOrders(ThemeData theme) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          children: [
            Icon(
              Icons.inbox_outlined,
              size: 48,
              color: theme.colorScheme.outline,
            ),
            const SizedBox(height: 12),
            Text(
              'No orders yet',
              style: theme.textTheme.titleMedium,
            ),
            const SizedBox(height: 4),
            Text(
              'Orders will appear here once customers place them.',
              style: theme.textTheme.bodySmall?.copyWith(
                color: AppTheme.textSecondary,
              ),
              textAlign: TextAlign.center,
            ),
          ],
        ),
      ),
    );
  }
}

class _MetricData {
  final IconData icon;
  final String label;
  final String value;
  final Color color;
  final VoidCallback? onTap;

  const _MetricData({
    required this.icon,
    required this.label,
    required this.value,
    required this.color,
    this.onTap,
  });
}

class _MetricCard extends StatelessWidget {
  final _MetricData data;

  const _MetricCard({required this.data});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return InkWell(
      onTap: data.onTap,
      borderRadius: BorderRadius.circular(12),
      child: Container(
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: data.color.withValues(alpha: 0.08),
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: data.color.withValues(alpha: 0.15)),
        ),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(data.icon, color: data.color, size: 28),
            const SizedBox(height: 8),
            Text(
              data.value,
              style: theme.textTheme.titleMedium?.copyWith(
                color: data.color,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 2),
            Text(
              data.label,
              style: theme.textTheme.bodySmall?.copyWith(
                color: AppTheme.textSecondary,
              ),
              textAlign: TextAlign.center,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
            ),
          ],
        ),
      ),
    );
  }
}

class _ActionData {
  final IconData icon;
  final String label;
  final VoidCallback onTap;

  const _ActionData({
    required this.icon,
    required this.label,
    required this.onTap,
  });
}

class _ActionCard extends StatelessWidget {
  final _ActionData data;

  const _ActionCard({required this.data});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return InkWell(
      onTap: data.onTap,
      borderRadius: BorderRadius.circular(12),
      child: Container(
        padding: const EdgeInsets.symmetric(vertical: 12, horizontal: 8),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: const Color(0xFFE0E0E0)),
        ),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(data.icon, color: AppTheme.primaryColor, size: 28),
            const SizedBox(height: 8),
            Text(
              data.label,
              style: theme.textTheme.labelLarge,
              textAlign: TextAlign.center,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
            ),
          ],
        ),
      ),
    );
  }
}

class _OrderStatCard extends StatelessWidget {
  final OrderSummaryModel order;

  const _OrderStatCard({required this.order});

  Color _statusColor(OrderStatus status) {
    switch (status) {
      case OrderStatus.completed:
      case OrderStatus.customerConfirmed:
        return AppTheme.secondaryColor;
      case OrderStatus.cancelled:
        return AppTheme.errorColor;
      case OrderStatus.delivered:
      case OrderStatus.outForDelivery:
        return AppTheme.accentColor;
      default:
        return AppTheme.primaryColor;
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final statusColor = _statusColor(order.status);

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: ListTile(
        leading: CircleAvatar(
          backgroundColor: statusColor.withValues(alpha: 0.1),
          child: Icon(
            Icons.local_laundry_service,
            color: statusColor,
          ),
        ),
        title: Text(
          order.orderNumber,
          style: theme.textTheme.labelLarge,
        ),
        subtitle: Text(
          order.status.displayName,
          style: theme.textTheme.bodySmall?.copyWith(
            color: statusColor,
            fontWeight: FontWeight.w600,
          ),
        ),
        trailing: Text(
          '\$${order.totalAmount.toStringAsFixed(2)}',
          style: theme.textTheme.titleMedium?.copyWith(
            color: AppTheme.primaryColor,
            fontWeight: FontWeight.bold,
          ),
        ),
        onTap: () => context.push('${AppRoutes.adminOrderDetail}/${order.id}'),
      ),
    );
  }
}