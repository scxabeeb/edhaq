import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../../core/data/models/order_models.dart';
import '../../core/di/injection.dart';
import '../../core/router/app_router.dart';
import '../../core/theme/app_theme.dart';
import '../../core/usecases/order_usecases.dart';

class OrdersScreen extends StatefulWidget {
  const OrdersScreen({super.key});

  @override
  State<OrdersScreen> createState() => _OrdersScreenState();
}

class _OrdersScreenState extends State<OrdersScreen> {
  List<OrderSummaryModel> _orders = [];
  bool _isLoading = true;
  String? _error;
  int _page = 1;
  bool _hasMore = true;
  bool _loadingMore = false;

  @override
  void initState() {
    super.initState();
    _loadOrders();
  }

  Future<void> _loadOrders() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });

    final result = await sl<GetOrdersUseCase>()(
      const GetOrdersParams(page: 1, pageSize: 20),
    );

    if (!mounted) return;

    result.fold(
      (failure) {
        setState(() {
          _isLoading = false;
          _error = failure.message;
        });
      },
      (response) {
        setState(() {
          _orders = response.orders;
          _page = 1;
          _hasMore = response.totalPages > 1;
          _isLoading = false;
        });
      },
    );
  }

  Future<void> _loadMore() async {
    if (_loadingMore || !_hasMore) return;

    setState(() => _loadingMore = true);

    final result = await sl<GetOrdersUseCase>()(
      GetOrdersParams(page: _page + 1, pageSize: 20),
    );

    if (!mounted) return;

    result.fold(
      (failure) {
        setState(() => _loadingMore = false);
      },
      (response) {
        setState(() {
          _orders = [..._orders, ...response.orders];
          _page++;
          _hasMore = _page < response.totalPages;
          _loadingMore = false;
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
        title: const Text('My Orders'),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.pop(),
        ),
      ),
      body: RefreshIndicator(
        onRefresh: _loadOrders,
        child: _isLoading
            ? const Center(child: CircularProgressIndicator())
            : _error != null
                ? _buildError(theme)
                : _orders.isEmpty
                    ? _buildEmpty(theme)
                    : _buildOrdersList(theme),
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => context.push(AppRoutes.createOrder),
        icon: const Icon(Icons.add),
        label: const Text('New Order'),
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
            onPressed: _loadOrders,
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
          Icons.inbox_outlined,
          size: 64,
          color: theme.colorScheme.outline,
        ),
        const SizedBox(height: 16),
        Text(
          'No orders yet',
          textAlign: TextAlign.center,
          style: theme.textTheme.titleLarge,
        ),
        const SizedBox(height: 8),
        Text(
          'Place your first laundry order!',
          textAlign: TextAlign.center,
          style: theme.textTheme.bodyMedium,
        ),
      ],
    );
  }

  Widget _buildOrdersList(ThemeData theme) {
    return ListView.builder(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.all(16),
      itemCount: _orders.length + (_hasMore ? 1 : 0),
      itemBuilder: (context, index) {
        if (index >= _orders.length) {
          return Padding(
            padding: const EdgeInsets.all(16),
            child: Center(
              child: _loadingMore
                  ? const CircularProgressIndicator()
                  : TextButton(
                      onPressed: _loadMore,
                      child: const Text('Load More'),
                    ),
            ),
          );
        }

        final order = _orders[index];
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
            subtitle: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const SizedBox(height: 4),
                Text(
                  order.status.displayName,
                  style: theme.textTheme.bodySmall?.copyWith(
                    color: _statusColor(order.status),
                    fontWeight: FontWeight.w600,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  _formatDate(order.createdAt),
                  style: theme.textTheme.bodySmall,
                ),
              ],
            ),
            trailing: Text(
              '\$${order.totalAmount.toStringAsFixed(2)}',
              style: theme.textTheme.titleMedium?.copyWith(
                color: AppTheme.primaryColor,
                fontWeight: FontWeight.bold,
              ),
            ),
            onTap: () => context.push('${AppRoutes.orderDetail}/${order.id}'),
          ),
        );
      },
    );
  }

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