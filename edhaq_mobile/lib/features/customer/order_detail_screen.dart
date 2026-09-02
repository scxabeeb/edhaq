import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:fluttertoast/fluttertoast.dart';
import 'package:go_router/go_router.dart';
import '../../core/data/models/order_models.dart';
import '../../core/di/injection.dart';
import '../../core/theme/app_theme.dart';
import '../../core/usecases/order_usecases.dart';

class OrderDetailScreen extends StatefulWidget {
  final int orderId;

  const OrderDetailScreen({super.key, required this.orderId});

  @override
  State<OrderDetailScreen> createState() => _OrderDetailScreenState();
}

class _OrderDetailScreenState extends State<OrderDetailScreen> {
  OrderDetailModel? _order;
  bool _isLoading = true;
  String? _error;
  bool _confirming = false;
  bool _paying = false;

  /// USSD short-code for clearing the payment before delivery, per payment
  /// method. e.g. Sahal: *884*442628*25#
  static String _amountPart(double amount) =>
      amount.toStringAsFixed(amount.truncateToDouble() == amount ? 0 : 2);

  static String _buildUssdCode(PaymentMethod method, double amount) =>
      switch (method) {
        PaymentMethod.sahal => '*884*442628*${_amountPart(amount)}#',
        // Other methods: short code to be provided later.
        _ => '',
      };

  @override
  void initState() {
    super.initState();
    _loadOrder();
  }

  Future<void> _loadOrder() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });

    final result = await sl<GetOrderUseCase>()(widget.orderId);

    if (!mounted) return;

    result.fold(
      (failure) {
        setState(() {
          _isLoading = false;
          _error = failure.message;
        });
      },
      (order) {
        setState(() {
          _order = order;
          _isLoading = false;
        });
      },
    );
  }

  Future<void> _confirmDelivery() async {
    setState(() => _confirming = true);

    final result = await sl<ConfirmDeliveryUseCase>()(widget.orderId);

    if (!mounted) return;

    result.fold(
      (failure) {
        setState(() => _confirming = false);
        Fluttertoast.showToast(
          msg: failure.message,
          toastLength: Toast.LENGTH_LONG,
          gravity: ToastGravity.BOTTOM,
          backgroundColor: Colors.red.shade700,
          textColor: Colors.white,
        );
      },
      (order) {
        setState(() {
          _order = order;
          _confirming = false;
        });
        Fluttertoast.showToast(
          msg: 'Delivery confirmed!',
          toastLength: Toast.LENGTH_SHORT,
          gravity: ToastGravity.BOTTOM,
          backgroundColor: AppTheme.secondaryColor,
          textColor: Colors.white,
        );
      },
    );
  }

  Future<void> _payOrder() async {
    setState(() => _paying = true);

    final result =
        await sl<PayOrderUseCase>()(PayOrderArgs(orderId: widget.orderId));

    if (!mounted) return;

    result.fold(
      (failure) {
        setState(() => _paying = false);
        Fluttertoast.showToast(
          msg: failure.message,
          toastLength: Toast.LENGTH_LONG,
          gravity: ToastGravity.BOTTOM,
          backgroundColor: Colors.red.shade700,
          textColor: Colors.white,
        );
      },
      (_) {
        Fluttertoast.showToast(
          msg: 'Payment cleared! Delivery can now proceed.',
          toastLength: Toast.LENGTH_LONG,
          gravity: ToastGravity.BOTTOM,
          backgroundColor: AppTheme.secondaryColor,
          textColor: Colors.white,
        );
        _loadOrder();
      },
    );
  }

  void _copyUssdCode(String code) {
    Clipboard.setData(ClipboardData(text: code));
    Fluttertoast.showToast(
      msg: 'USSD code copied: $code',
      toastLength: Toast.LENGTH_SHORT,
      gravity: ToastGravity.BOTTOM,
      backgroundColor: AppTheme.primaryColor,
      textColor: Colors.white,
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      backgroundColor: theme.colorScheme.surface,
      appBar: AppBar(
        title: Text(_order?.orderNumber ?? 'Order Details'),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.pop(),
        ),
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? _buildError(theme)
              : _buildOrderDetail(theme),
    );
  }

  Widget _buildError(ThemeData theme) {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(Icons.error_outline, size: 64, color: theme.colorScheme.error),
          const SizedBox(height: 16),
          Text(
            _error ?? 'Something went wrong',
            textAlign: TextAlign.center,
            style: theme.textTheme.bodyLarge,
          ),
          const SizedBox(height: 24),
          ElevatedButton(
            onPressed: _loadOrder,
            child: const Text('Retry'),
          ),
        ],
      ),
    );
  }

  Widget _buildOrderDetail(ThemeData theme) {
    final order = _order!;

    return RefreshIndicator(
      onRefresh: _loadOrder,
      child: ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.all(16),
        children: [
          // Status card
          Card(
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Icon(
                        _statusIcon(order.status),
                        color: _statusColor(order.status),
                        size: 32,
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              order.status.displayName,
                              style: theme.textTheme.titleMedium?.copyWith(
                                color: _statusColor(order.status),
                              ),
                            ),
                            const SizedBox(height: 4),
                            Text(
                              'Order #${order.orderNumber}',
                              style: theme.textTheme.bodySmall,
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 16),
                  Row(
                    children: [
                      _InfoChip(
                        icon: Icons.payment,
                        label: 'Payment',
                        value: order.paymentStatus.displayName,
                      ),
                      const SizedBox(width: 8),
                      _InfoChip(
                        icon: Icons.credit_card,
                        label: 'Method',
                        value: order.paymentMethod.displayName,
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
          // USSD payment card (shown until the payment is cleared)
          if (order.paymentStatus != PaymentStatus.paid) ...[
            _UssdPaymentCard(
              amount: order.totalAmount,
              ussdCode:
                  _buildUssdCode(order.paymentMethod, order.totalAmount),
              onCopy: () => _copyUssdCode(
                  _buildUssdCode(order.paymentMethod, order.totalAmount)),
              onPaid: _payOrder,
              paying: _paying,
            ),
            const SizedBox(height: 16),
          ],

          // Order flow progress stepper
          _OrderFlowStepper(status: order.status),
          const SizedBox(height: 16),

          // Pickup & delivery schedule
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Schedule', style: theme.textTheme.titleMedium),
                  const SizedBox(height: 12),
                  _ScheduleRow(
                    icon: Icons.local_shipping_outlined,
                    label: 'Pickup',
                    scheduled: order.pickupScheduledAt,
                    actual: order.pickupActualAt,
                  ),
                  const Divider(height: 24),
                  _ScheduleRow(
                    icon: Icons.home_outlined,
                    label: 'Delivery',
                    scheduled: order.deliveryScheduledAt,
                    actual: order.deliveryActualAt,
                  ),
                  const Divider(height: 24),
                  _ScheduleRow(
                    icon: Icons.event_available_outlined,
                    label: 'Estimated Completion',
                    scheduled: order.estimatedCompletionAt,
                    actual: null,
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),

          // Assigned drivers
          if (order.driverAssignments.isNotEmpty) ...[
            Text('Assigned Drivers', style: theme.textTheme.titleMedium),
            const SizedBox(height: 8),
            ...order.driverAssignments.map(
              (driver) => _DriverCard(driver: driver),
            ),
            const SizedBox(height: 16),
          ],

          // Addresses
          if (order.pickupAddress != null) ...[
            _AddressCard(
              icon: Icons.local_shipping_outlined,
              title: 'Pickup Address',
              address: order.pickupAddress!,
            ),
            const SizedBox(height: 12),
          ],
          if (order.deliveryAddress != null) ...[
            _AddressCard(
              icon: Icons.home_outlined,
              title: 'Delivery Address',
              address: order.deliveryAddress!,
            ),
            const SizedBox(height: 16),
          ],

          // Items
          Text('Items', style: theme.textTheme.titleMedium),
          const SizedBox(height: 8),
          Card(
            child: Column(
              children: order.items
                  .map((item) => ListTile(
                        leading: CircleAvatar(
                          backgroundColor:
                              AppTheme.primaryColor.withValues(alpha: 0.1),
                          child: const Icon(
                            Icons.checkroom,
                            color: AppTheme.primaryColor,
                            size: 20,
                          ),
                        ),
                        title: Text(
                          item.serviceName ?? 'Service',
                          style: theme.textTheme.labelLarge,
                        ),
                        subtitle: Text(
                          'Qty: ${item.quantity} × \$${item.unitPrice.toStringAsFixed(2)}',
                          style: theme.textTheme.bodySmall,
                        ),
                        trailing: Text(
                          '\$${item.totalPrice.toStringAsFixed(2)}',
                          style: theme.textTheme.titleMedium?.copyWith(
                            color: AppTheme.primaryColor,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ))
                  .toList(),
            ),
          ),
          const SizedBox(height: 16),

          // Price summary
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                children: [
                  _PriceRow(label: 'Subtotal', value: order.subTotal),
                  const SizedBox(height: 8),
                  _PriceRow(label: 'Delivery Fee', value: order.deliveryFee),
                  if (order.discount > 0) ...[
                    const SizedBox(height: 8),
                    _PriceRow(
                      label: 'Discount',
                      value: -order.discount,
                      color: AppTheme.secondaryColor,
                    ),
                  ],
                  const Divider(height: 24),
                  _PriceRow(
                    label: 'Total',
                    value: order.totalAmount,
                    isBold: true,
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),

          // Tracking timeline
          if (order.trackings.isNotEmpty) ...[
            Text('Tracking', style: theme.textTheme.titleMedium),
            const SizedBox(height: 8),
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  children: order.trackings
                      .map((tracking) => _TrackingItem(tracking: tracking))
                      .toList(),
                ),
              ),
            ),
            const SizedBox(height: 16),
          ],

          // Confirm delivery button
          if (order.status == OrderStatus.delivered) ...[
            ElevatedButton(
              onPressed: _confirming ? null : _confirmDelivery,
              child: _confirming
                  ? const SizedBox(
                      width: 24,
                      height: 24,
                      child: CircularProgressIndicator(
                        color: Colors.white,
                        strokeWidth: 2,
                      ),
                    )
                  : const Text('Confirm Delivery'),
            ),
            const SizedBox(height: 16),
          ],
        ],
      ),
    );
  }

  IconData _statusIcon(OrderStatus status) {
    switch (status) {
      case OrderStatus.completed:
      case OrderStatus.customerConfirmed:
        return Icons.check_circle;
      case OrderStatus.cancelled:
        return Icons.cancel;
      case OrderStatus.delivered:
      case OrderStatus.outForDelivery:
        return Icons.local_shipping;
      case OrderStatus.washing:
      case OrderStatus.dryCleaning:
      case OrderStatus.drying:
      case OrderStatus.ironing:
      case OrderStatus.folding:
      case OrderStatus.packaging:
      case OrderStatus.sorting:
        return Icons.local_laundry_service;
      default:
        return Icons.schedule;
    }
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
}

class _OrderFlowStepper extends StatelessWidget {
  final OrderStatus status;

  const _OrderFlowStepper({required this.status});

  static const _stages = <OrderStatus>[
    OrderStatus.orderPlaced,
    OrderStatus.driverAssigned,
    OrderStatus.clothesPickedUp,
    OrderStatus.washing,
    OrderStatus.readyForDelivery,
    OrderStatus.outForDelivery,
    OrderStatus.delivered,
  ];

  int get _currentIndex {
    if (status == OrderStatus.cancelled) return -1;
    if (status == OrderStatus.completed ||
        status == OrderStatus.customerConfirmed) {
      return _stages.length - 1;
    }
    const laundryStages = [
      OrderStatus.sorting,
      OrderStatus.washing,
      OrderStatus.dryCleaning,
      OrderStatus.drying,
      OrderStatus.ironing,
      OrderStatus.folding,
      OrderStatus.packaging,
    ];
    if (laundryStages.contains(status)) return 3;
    final index = _stages.indexOf(status);
    return index < 0 ? 0 : index;
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final cancelled = status == OrderStatus.cancelled;

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Order Progress', style: theme.textTheme.titleMedium),
            const SizedBox(height: 16),
            SizedBox(
              height: 84,
              child: Row(
                children: List.generate(_stages.length * 2 - 1, (i) {
                  if (i.isOdd) {
                    final done = !cancelled && (i ~/ 2) < _currentIndex;
                    return Expanded(
                      child: Container(
                        height: 2,
                        color: done
                            ? AppTheme.secondaryColor
                            : theme.colorScheme.outline.withValues(alpha: 0.3),
                      ),
                    );
                  }
                  final stageIndex = i ~/ 2;
                  final stage = _stages[stageIndex];
                  final done = !cancelled && stageIndex <= _currentIndex;
                  final isCurrent = !cancelled && stageIndex == _currentIndex;
                  return Column(
                    children: [
                      Icon(
                        cancelled
                            ? Icons.cancel
                            : done
                                ? Icons.check_circle
                                : Icons.radio_button_unchecked,
                        size: 24,
                        color: cancelled
                            ? AppTheme.errorColor
                            : done
                                ? AppTheme.secondaryColor
                                : theme.colorScheme.outline,
                      ),
                      const SizedBox(height: 4),
                      Expanded(
                        child: Text(
                          _shortLabel(stage),
                          style: theme.textTheme.bodySmall?.copyWith(
                            fontSize: 9,
                            color: isCurrent
                                ? AppTheme.primaryColor
                                : AppTheme.textSecondary,
                            fontWeight:
                                isCurrent ? FontWeight.bold : FontWeight.normal,
                          ),
                          textAlign: TextAlign.center,
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                    ],
                  );
                }),
              ),
            ),
          ],
        ),
      ),
    );
  }

  String _shortLabel(OrderStatus stage) {
    switch (stage) {
      case OrderStatus.orderPlaced:
        return 'Placed';
      case OrderStatus.driverAssigned:
        return 'Driver Assigned';
      case OrderStatus.clothesPickedUp:
        return 'Picked Up';
      case OrderStatus.washing:
        return 'Cleaning';
      case OrderStatus.readyForDelivery:
        return 'Ready';
      case OrderStatus.outForDelivery:
        return 'On the Way';
      case OrderStatus.delivered:
        return 'Delivered';
      default:
        return '';
    }
  }
}

class _UssdPaymentCard extends StatelessWidget {
  final double amount;
  final String ussdCode;
  final VoidCallback onCopy;
  final VoidCallback onPaid;
  final bool paying;

  const _UssdPaymentCard({
    required this.amount,
    required this.ussdCode,
    required this.onCopy,
    required this.onPaid,
    required this.paying,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Card(
      color: AppTheme.accentColor.withValues(alpha: 0.08),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Icon(Icons.phone_android,
                    color: AppTheme.accentColor),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    'Clear payment to allow delivery',
                    style: theme.textTheme.titleMedium?.copyWith(
                      color: AppTheme.accentColor,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 8),
            Text(
              'Your delivery driver cannot complete the delivery until this '
              'order is paid. Dial the code below on your phone to pay '
              '\$${amount.toStringAsFixed(2)}, then tap "I have paid".',
              style: theme.textTheme.bodySmall,
            ),
            const SizedBox(height: 12),
            if (ussdCode.isEmpty)
              Text(
                'USSD payment code is not available yet for this payment method.',
                textAlign: TextAlign.center,
                style: theme.textTheme.bodyMedium?.copyWith(
                  color: AppTheme.textSecondary,
                ),
              )
            else
              Container(
                width: double.infinity,
                padding: const EdgeInsets.symmetric(vertical: 12),
                decoration: BoxDecoration(
                  color: theme.colorScheme.surface,
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(
                    color: AppTheme.accentColor.withValues(alpha: 0.4),
                  ),
                ),
                child: Text(
                  ussdCode,
                  textAlign: TextAlign.center,
                  style: theme.textTheme.titleLarge?.copyWith(
                    fontWeight: FontWeight.bold,
                    letterSpacing: 1.5,
                    color: AppTheme.primaryColor,
                  ),
                ),
              ),
            const SizedBox(height: 12),
            Row(
              children: [
                Expanded(
                  child: OutlinedButton.icon(
                    onPressed: ussdCode.isEmpty ? null : onCopy,
                    icon: const Icon(Icons.copy, size: 18),
                    label: const Text('Copy Code'),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: ElevatedButton.icon(
                    onPressed: paying ? null : onPaid,
                    icon: paying
                        ? const SizedBox(
                            width: 18,
                            height: 18,
                            child: CircularProgressIndicator(
                              strokeWidth: 2,
                              color: Colors.white,
                            ),
                          )
                        : const Icon(Icons.check_circle_outline, size: 18),
                    label: const Text('I have paid'),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _ScheduleRow extends StatelessWidget {
  final IconData icon;
  final String label;
  final DateTime scheduled;
  final DateTime? actual;

  const _ScheduleRow({
    required this.icon,
    required this.label,
    required this.scheduled,
    required this.actual,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Row(
      children: [
        Icon(icon, size: 20, color: AppTheme.primaryColor),
        const SizedBox(width: 12),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(label, style: theme.textTheme.labelLarge),
              const SizedBox(height: 2),
              Text(
                actual != null
                    ? 'Completed: ${_format(actual!)}'
                    : 'Scheduled: ${_format(scheduled)}',
                style: theme.textTheme.bodySmall?.copyWith(
                  color: actual != null
                      ? AppTheme.secondaryColor
                      : AppTheme.textSecondary,
                  fontWeight:
                      actual != null ? FontWeight.w600 : FontWeight.normal,
                ),
              ),
            ],
          ),
        ),
        if (actual != null)
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
            decoration: BoxDecoration(
              color: AppTheme.secondaryColor.withValues(alpha: 0.1),
              borderRadius: BorderRadius.circular(12),
            ),
            child: Text(
              'Done',
              style: theme.textTheme.bodySmall?.copyWith(
                color: AppTheme.secondaryColor,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
      ],
    );
  }

  String _format(DateTime date) {
    final local = date.toLocal();
    final hour = local.hour.toString().padLeft(2, '0');
    final minute = local.minute.toString().padLeft(2, '0');
    return '${local.month}/${local.day}/${local.year} $hour:$minute';
  }
}

class _DriverCard extends StatelessWidget {
  final DriverAssignmentModel driver;

  const _DriverCard({required this.driver});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isPickup = driver.isPickup;

    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Row(
          children: [
            CircleAvatar(
              backgroundColor: (isPickup
                      ? AppTheme.accentColor
                      : AppTheme.secondaryColor)
                  .withValues(alpha: 0.1),
              child: Icon(
                isPickup ? Icons.local_taxi : Icons.home,
                color:
                    isPickup ? AppTheme.accentColor : AppTheme.secondaryColor,
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    '${isPickup ? 'Pickup' : 'Delivery'} Driver',
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: isPickup
                          ? AppTheme.accentColor
                          : AppTheme.secondaryColor,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  Text(
                    driver.driverName ?? 'Assigning...',
                    style: theme.textTheme.labelLarge,
                  ),
                  if (driver.phoneNumber != null &&
                      driver.phoneNumber!.isNotEmpty)
                    Text(
                      driver.phoneNumber!,
                      style: theme.textTheme.bodySmall,
                    ),
                  if ((driver.vehicleModel != null &&
                          driver.vehicleModel!.isNotEmpty) ||
                      (driver.licensePlate != null &&
                          driver.licensePlate!.isNotEmpty))
                    Text(
                      [
                        if (driver.vehicleModel != null &&
                            driver.vehicleModel!.isNotEmpty)
                          driver.vehicleModel!,
                        if (driver.licensePlate != null &&
                            driver.licensePlate!.isNotEmpty)
                          'Plate: ${driver.licensePlate!}',
                      ].join(' · '),
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: AppTheme.textSecondary,
                      ),
                    ),
                  Text(
                    'Status: ${driver.status.displayName}',
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: driver.status == DriverJobAction.completed
                          ? AppTheme.secondaryColor
                          : AppTheme.textSecondary,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _InfoChip extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;

  const _InfoChip({
    required this.icon,
    required this.label,
    required this.value,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Expanded(
      child: Container(
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: AppTheme.surfaceColor,
          borderRadius: BorderRadius.circular(8),
        ),
        child: Row(
          children: [
            Icon(icon, size: 20, color: AppTheme.primaryColor),
            const SizedBox(width: 8),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    label,
                    style: theme.textTheme.bodySmall,
                  ),
                  Text(
                    value,
                    style: theme.textTheme.labelLarge,
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _AddressCard extends StatelessWidget {
  final IconData icon;
  final String title;
  final AddressSummaryModel address;

  const _AddressCard({
    required this.icon,
    required this.title,
    required this.address,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Card(
      child: ListTile(
        leading: CircleAvatar(
          backgroundColor: AppTheme.primaryColor.withValues(alpha: 0.1),
          child: Icon(icon, color: AppTheme.primaryColor),
        ),
        title: Text(title, style: theme.textTheme.labelLarge),
        subtitle: Text(
          address.fullAddress,
          style: theme.textTheme.bodySmall,
        ),
      ),
    );
  }
}

class _PriceRow extends StatelessWidget {
  final String label;
  final double value;
  final bool isBold;
  final Color? color;

  const _PriceRow({
    required this.label,
    required this.value,
    this.isBold = false,
    this.color,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final style = isBold
        ? theme.textTheme.titleMedium?.copyWith(
            fontWeight: FontWeight.bold,
            color: color ?? AppTheme.textPrimary,
          )
        : theme.textTheme.bodyMedium?.copyWith(color: color);

    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(label, style: style),
        Text(
          '\$${value.toStringAsFixed(2)}',
          style: style,
        ),
      ],
    );
  }
}

class _TrackingItem extends StatelessWidget {
  final OrderTrackingModel tracking;

  const _TrackingItem({required this.tracking});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(
            Icons.circle,
            size: 12,
            color: AppTheme.primaryColor,
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  tracking.status.displayName,
                  style: theme.textTheme.labelLarge,
                ),
                if (tracking.note != null && tracking.note!.isNotEmpty) ...[
                  const SizedBox(height: 2),
                  Text(
                    tracking.note!,
                    style: theme.textTheme.bodySmall,
                  ),
                ],
                const SizedBox(height: 2),
                Text(
                  _formatDateTime(tracking.createdAt),
                  style: theme.textTheme.bodySmall?.copyWith(
                    color: AppTheme.textSecondary,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  String _formatDateTime(DateTime date) {
    final local = date.toLocal();
    final hour = local.hour.toString().padLeft(2, '0');
    final minute = local.minute.toString().padLeft(2, '0');
    return '${local.month}/${local.day}/${local.year} $hour:$minute';
  }
}