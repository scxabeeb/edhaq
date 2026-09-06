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

  // Tab filter: 0 = New, 1 = Ongoing, 2 = Completed.
  int _tab = 0;

  static const _tabLabels = ['New', 'Ongoing', 'Completed'];

  List<OrderSummaryModel> get _filteredOrders {
    Iterable<OrderSummaryModel> filtered = _orders.where((o) {
      switch (_tab) {
        case 0: // New — just placed / awaiting pickup
          return o.status == OrderStatus.orderPlaced ||
              o.status == OrderStatus.pickupScheduled ||
              o.status == OrderStatus.driverAssigned;
        case 1: // Ongoing — driver en route through delivery
          return o.status == OrderStatus.driverOnTheWay ||
              o.status == OrderStatus.clothesPickedUp ||
              o.status == OrderStatus.laundryReceived ||
              o.status == OrderStatus.sorting ||
              o.status == OrderStatus.washing ||
              o.status == OrderStatus.dryCleaning ||
              o.status == OrderStatus.drying ||
              o.status == OrderStatus.ironing ||
              o.status == OrderStatus.folding ||
              o.status == OrderStatus.packaging ||
              o.status == OrderStatus.readyForDelivery ||
              o.status == OrderStatus.outForDelivery ||
              o.status == OrderStatus.delivered;
        case 2: // Completed (and cancelled)
        default:
          return o.status.isTerminal;
      }
    });
    final list = filtered.toList();
    list.sort((a, b) => b.createdAt.compareTo(a.createdAt));
    return list;
  }

  int _countForTab(int tab) {
    final saved = _tab;
    _tab = tab;
    final count = _filteredOrders.length;
    _tab = saved;
    return count;
  }

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
      body: Column(
        children: [
          // Tab filter bar
          Container(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
            child: Row(
              children: List.generate(_tabLabels.length, (i) {
                final selected = _tab == i;
                final count = _isLoading ? 0 : _countForTab(i);
                return Expanded(
                  child: Padding(
                    padding: EdgeInsets.only(right: i < _tabLabels.length - 1 ? 8 : 0),
                    child: ChoiceChip(
                      label: Text(
                        count > 0 ? '${_tabLabels[i]} ($count)' : _tabLabels[i],
                        style: TextStyle(
                          fontSize: 12,
                          fontWeight: FontWeight.w600,
                          color: selected ? Colors.white : null,
                        ),
                      ),
                      selected: selected,
                      selectedColor: AppTheme.primaryColor,
                      showCheckmark: false,
                      visualDensity: VisualDensity.compact,
                      onSelected: (_) => setState(() => _tab = i),
                    ),
                  ),
                );
              }),
            ),
          ),
          Expanded(
            child: RefreshIndicator(
              onRefresh: _loadOrders,
              child: _isLoading
                  ? const Center(child: CircularProgressIndicator())
                  : _error != null
                      ? _buildError(theme)
                      : _filteredOrders.isEmpty
                          ? _buildEmpty(theme)
                          : _buildOrdersList(theme),
            ),
          ),
        ],
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
    final messages = [
      'No new orders.\nPlace your first laundry order!',
      'No ongoing orders right now.',
      'No completed orders yet.',
    ];
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
          messages[_tab],
          textAlign: TextAlign.center,
          style: theme.textTheme.titleLarge,
        ),
        if (_tab == 0) ...[
          const SizedBox(height: 8),
          Text(
            'Tap + New Order below to get started.',
            textAlign: TextAlign.center,
            style: theme.textTheme.bodyMedium,
          ),
        ],
      ],
    );
  }

  Widget _buildOrdersList(ThemeData theme) {
    final visible = _filteredOrders;
    return ListView.builder(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.all(16),
      itemCount: visible.length + (_tab == _tabLabels.length - 1 && _hasMore ? 1 : 0),
      itemBuilder: (context, index) {
        if (index >= visible.length) {
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

        final order = visible[index];
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