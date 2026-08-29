import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../../core/data/models/order_models.dart';
import '../../core/di/injection.dart';
import '../../core/router/app_router.dart';
import '../../core/theme/app_theme.dart';
import '../../core/usecases/order_usecases.dart';

/// Filter preset passed from the admin dashboard cards via route `extra`.
class AdminOrdersFilter {
  final OrderStatus? status;
  final bool activeOnly;

  const AdminOrdersFilter({this.status, this.activeOnly = false});
}

class AdminOrdersScreen extends StatefulWidget {
  final AdminOrdersFilter? initialFilter;

  const AdminOrdersScreen({super.key, this.initialFilter});

  @override
  State<AdminOrdersScreen> createState() => _AdminOrdersScreenState();
}

class _AdminOrdersScreenState extends State<AdminOrdersScreen> {
  List<OrderSummaryModel> _orders = [];
  bool _isLoading = true;
  String? _error;
  bool _loadingMore = false;
  int _page = 1;
  bool _hasMore = true;

  final _searchController = TextEditingController();
  OrderStatus? _selectedStatus;
  bool _activeOnly = false;

  @override
  void initState() {
    super.initState();
    _selectedStatus = widget.initialFilter?.status;
    _activeOnly = widget.initialFilter?.activeOnly ?? false;
    _loadOrders();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _loadOrders({bool refresh = false}) async {
    if (refresh) {
      _page = 1;
      _orders = [];
      _hasMore = true;
    }

    setState(() {
      _isLoading = !refresh || _orders.isEmpty;
      _error = null;
    });

    final result = await sl<GetAdminOrdersUseCase>()(
      GetAdminOrdersParams(
        page: _page,
        pageSize: 20,
        search: _searchController.text.trim().isEmpty
            ? null
            : _searchController.text.trim(),
        statusFilter: _selectedStatus,
        activeOnly: _activeOnly ? true : null,
      ),
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
          _orders = refresh
              ? response.orders
              : [..._orders, ...response.orders];
          _page++;
          _hasMore = _page < response.totalPages;
          _isLoading = false;
        });
      },
    );
  }

  Future<void> _loadMore() async {
    if (_loadingMore || !_hasMore) return;

    setState(() => _loadingMore = true);

    final result = await sl<GetAdminOrdersUseCase>()(
      GetAdminOrdersParams(
        page: _page,
        pageSize: 20,
        search: _searchController.text.trim().isEmpty
            ? null
            : _searchController.text.trim(),
        statusFilter: _selectedStatus,
        activeOnly: _activeOnly ? true : null,
      ),
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

  void _applyFilters() {
    _loadOrders(refresh: true);
  }

  void _clearFilters() {
    _searchController.clear();
    setState(() {
      _selectedStatus = null;
      _activeOnly = false;
    });
    _loadOrders(refresh: true);
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

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      backgroundColor: theme.colorScheme.surface,
      appBar: AppBar(
        title: const Text('All Orders'),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.pop(),
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: () => _loadOrders(refresh: true),
            tooltip: 'Refresh',
          ),
        ],
      ),
      body: Column(
        children: [
          // Search & filter bar
          Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              children: [
                TextField(
                  controller: _searchController,
                  decoration: InputDecoration(
                    hintText: 'Search by order number or customer name...',
                    prefixIcon: const Icon(Icons.search),
                    suffixIcon: _searchController.text.isNotEmpty
                        ? IconButton(
                            icon: const Icon(Icons.clear),
                            onPressed: _clearFilters,
                          )
                        : null,
                  ),
                  onSubmitted: (_) => _applyFilters(),
                ),
                const SizedBox(height: 12),
                DropdownButtonFormField<OrderStatus?>(
                  isExpanded: true,
                  initialValue: _selectedStatus,
                  decoration: const InputDecoration(
                    labelText: 'Status Filter',
                    prefixIcon: Icon(Icons.filter_alt_outlined),
                    isDense: true,
                  ),
                  items: [
                    const DropdownMenuItem(
                        value: null, child: Text('All Statuses')),
                    ...OrderStatus.values
                        .where((s) =>
                            s != OrderStatus.unknown &&
                            s != OrderStatus.customerConfirmed)
                        .map((s) => DropdownMenuItem(
                              value: s,
                              child: Text(
                                s.displayName,
                                overflow: TextOverflow.ellipsis,
                              ),
                            )),
                  ],
                   onChanged: (value) {
                     setState(() {
                       _selectedStatus = value;
                       if (value != null) _activeOnly = false;
                     });
                     _applyFilters();
                   },
                 ),
                const SizedBox(height: 12),
                SizedBox(
                  width: double.infinity,
                  child: ElevatedButton(
                    onPressed: _applyFilters,
                    child: const Text('Apply'),
                  ),
                ),
              ],
            ),
          ),

          // Orders list
          Expanded(
            child: RefreshIndicator(
              onRefresh: () => _loadOrders(refresh: true),
              child: _isLoading
                  ? const Center(child: CircularProgressIndicator())
                  : _error != null
                      ? _buildError(theme)
                      : _orders.isEmpty
                          ? _buildEmpty(theme)
                          : _buildOrdersList(theme),
            ),
          ),
        ],
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
            onPressed: () => _loadOrders(refresh: true),
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
          Icons.inventory_2_outlined,
          size: 64,
          color: theme.colorScheme.outline,
        ),
        const SizedBox(height: 16),
        Text(
          'No orders found',
          textAlign: TextAlign.center,
          style: theme.textTheme.titleLarge,
        ),
      ],
    );
  }

  Widget _buildOrdersList(ThemeData theme) {
    return ListView.builder(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.only(left: 16, right: 16, bottom: 16, top: 0),
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
      },
    );
  }
}
