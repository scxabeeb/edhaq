import 'package:flutter/material.dart';
import 'package:fluttertoast/fluttertoast.dart';
import 'package:go_router/go_router.dart';
import '../../core/data/models/address_models.dart';
import '../../core/data/models/order_models.dart';
import '../../core/data/models/service_models.dart';
import '../../core/di/injection.dart';
import '../../core/router/app_router.dart';
import '../../core/theme/app_theme.dart';
import '../../core/usecases/address_usecases.dart';
import '../../core/usecases/order_usecases.dart';
import '../../core/usecases/service_usecases.dart';
import '../../core/usecases/usecase.dart';

class CreateOrderScreen extends StatefulWidget {
  const CreateOrderScreen({super.key});

  @override
  State<CreateOrderScreen> createState() => _CreateOrderScreenState();
}

class _CreateOrderScreenState extends State<CreateOrderScreen> {
  final _formKey = GlobalKey<FormState>();
  final _specialInstructionsController = TextEditingController();
  final _couponController = TextEditingController();
  final _searchController = TextEditingController();

  bool _isLoading = false;
  bool _loadingData = true;
  bool _cartExpanded = false;
  String _error = '';
  String _searchQuery = '';

  // Data
  List<AddressModel> _addresses = [];
  List<ServiceCategoryModel> _categories = [];
  List<ServiceModel> _services = [];

  // Selections
  AddressModel? _pickupAddress;
  AddressModel? _deliveryAddress;
  ServiceCategoryModel? _selectedCategory;
  final Map<int, int> _selectedServices = {}; // serviceId -> quantity
  PaymentMethod _paymentMethod = PaymentMethod.cash;
  DateTime _pickupDate = DateTime.now().add(const Duration(days: 1));
  DateTime _deliveryDate = DateTime.now().add(const Duration(days: 3));

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  @override
  void dispose() {
    _specialInstructionsController.dispose();
    _couponController.dispose();
    _searchController.dispose();
    super.dispose();
  }

  /// Services filtered by the active category and search query.
  List<ServiceModel> get _filteredServices {
    final query = _searchQuery.trim().toLowerCase();
    return _services.where((s) {
      if (_selectedCategory != null && s.categoryId != _selectedCategory!.id) {
        return false;
      }
      if (query.isNotEmpty && !s.name.toLowerCase().contains(query)) {
        return false;
      }
      return true;
    }).toList();
  }

  List<MapEntry<ServiceModel, int>> get _cartEntries {
    return _selectedServices.entries
        .map((e) => MapEntry(
            _services.firstWhere((s) => s.id == e.key), e.value))
        .toList();
  }

  Future<void> _loadData() async {
    setState(() {
      _loadingData = true;
      _error = '';
    });

    // Load addresses
    final addressesResult = await sl<GetAddressesUseCase>()(const NoParams());
    if (!mounted) return;
    addressesResult.fold(
      (failure) {
        setState(() {
          _loadingData = false;
          _error = failure.message;
        });
      },
      (addresses) {
        setState(() => _addresses = addresses);
      },
    );

    // Load categories
    final categoriesResult =
        await sl<GetServiceCategoriesUseCase>()(const NoParams());
    if (!mounted) return;
    categoriesResult.fold(
      (failure) {
        setState(() {
          _loadingData = false;
          _error = failure.message;
        });
      },
      (categories) {
        setState(() => _categories = categories);
      },
    );

    // Load all services
    final servicesResult = await sl<GetServicesUseCase>()(null);
    if (!mounted) return;
    servicesResult.fold(
      (failure) {
        setState(() {
          _loadingData = false;
          _error = failure.message;
        });
      },
      (services) {
        setState(() {
          _services = services;
          _loadingData = false;
        });
      },
    );
  }

  Future<void> _selectPickupDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _pickupDate,
      firstDate: DateTime.now(),
      lastDate: DateTime.now().add(const Duration(days: 30)),
    );
    if (picked != null) {
      setState(() => _pickupDate = picked);
    }
  }

  Future<void> _selectDeliveryDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _deliveryDate,
      firstDate: _pickupDate,
      lastDate: DateTime.now().add(const Duration(days: 60)),
    );
    if (picked != null) {
      setState(() => _deliveryDate = picked);
    }
  }

  double get _subtotal {
    double total = 0;
    for (final entry in _selectedServices.entries) {
      final service = _services.firstWhere((s) => s.id == entry.key);
      total += service.pricePerPiece * entry.value;
    }
    return total;
  }

  double get _total => _subtotal;

  Future<void> _createOrder() async {
    if (!_formKey.currentState!.validate()) return;
    if (_pickupAddress == null || _deliveryAddress == null) {
      Fluttertoast.showToast(
        msg: 'Please select pickup and delivery addresses',
        toastLength: Toast.LENGTH_LONG,
        gravity: ToastGravity.BOTTOM,
        backgroundColor: Colors.red.shade700,
        textColor: Colors.white,
      );
      return;
    }
    if (_selectedServices.isEmpty) {
      Fluttertoast.showToast(
        msg: 'Please select at least one service',
        toastLength: Toast.LENGTH_LONG,
        gravity: ToastGravity.BOTTOM,
        backgroundColor: Colors.red.shade700,
        textColor: Colors.white,
      );
      return;
    }

    setState(() => _isLoading = true);

    final items = _selectedServices.entries
        .map((e) => CreateOrderItemRequest(
              serviceId: e.key,
              quantity: e.value,
            ))
        .toList();

    final request = CreateOrderRequest(
      pickupAddressId: _pickupAddress!.id,
      deliveryAddressId: _deliveryAddress!.id,
      pickupScheduledAt: _pickupDate,
      deliveryScheduledAt: _deliveryDate,
      items: items,
      couponCode: _couponController.text.trim().isEmpty
          ? null
          : _couponController.text.trim(),
      specialInstructions: _specialInstructionsController.text.trim().isEmpty
          ? null
          : _specialInstructionsController.text.trim(),
      paymentMethod: _paymentMethod,
    );

    final result = await sl<CreateOrderUseCase>()(request);

    if (!mounted) return;

    result.fold(
      (failure) {
        setState(() => _isLoading = false);
        Fluttertoast.showToast(
          msg: failure.message,
          toastLength: Toast.LENGTH_LONG,
          gravity: ToastGravity.BOTTOM,
          backgroundColor: Colors.red.shade700,
          textColor: Colors.white,
        );
      },
      (order) {
        setState(() => _isLoading = false);
        Fluttertoast.showToast(
          msg: 'Order created successfully!',
          toastLength: Toast.LENGTH_SHORT,
          gravity: ToastGravity.BOTTOM,
          backgroundColor: AppTheme.secondaryColor,
          textColor: Colors.white,
        );
        context.pushReplacement('${AppRoutes.orderDetail}/${order.id}');
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      backgroundColor: theme.colorScheme.surface,
      appBar: AppBar(
        title: const Text('New Order'),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.pop(),
        ),
      ),
      body: _loadingData
          ? const Center(child: CircularProgressIndicator())
          : _error.isNotEmpty
              ? _buildError(theme)
              : _buildForm(theme),
    );
  }

  /// Collapsible Selected Services / cart panel.
  Widget _buildCartPanel(ThemeData theme) {
    final entries = _cartEntries;
    final count = _selectedServices.length;
    final grandTotal = _subtotal;

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            InkWell(
              borderRadius: BorderRadius.circular(8),
              onTap: entries.isEmpty
                  ? null
                  : () => setState(() => _cartExpanded = !_cartExpanded),
              child: Row(
                children: [
                  Icon(
                    _cartExpanded
                        ? Icons.keyboard_arrow_up
                        : Icons.keyboard_arrow_down,
                    size: 20,
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      'Selected Services ($count)',
                      style: theme.textTheme.titleSmall
                          ?.copyWith(fontWeight: FontWeight.bold),
                    ),
                  ),
                  Text(
                    'Total: \$${grandTotal.toStringAsFixed(2)}',
                    style: theme.textTheme.titleSmall?.copyWith(
                      fontWeight: FontWeight.bold,
                      color: AppTheme.secondaryColor,
                    ),
                  ),
                ],
              ),
            ),
            if (_cartExpanded && entries.isNotEmpty) ...[
              const Divider(height: 24),
              ...entries.map((entry) {
                final service = entry.key;
                final qty = entry.value;
                final lineTotal = service.pricePerPiece * qty;
                return Padding(
                  padding: const EdgeInsets.symmetric(vertical: 6),
                  child: Row(
                    children: [
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(service.name,
                                style: theme.textTheme.labelLarge),
                            Text(
                              '${service.categoryName ?? 'Service'} · $qty × \$${service.pricePerPiece.toStringAsFixed(2)}',
                              style: theme.textTheme.bodySmall,
                            ),
                          ],
                        ),
                      ),
                      Padding(
                        padding: const EdgeInsets.only(right: 12),
                        child: Text(
                          '\$${lineTotal.toStringAsFixed(2)}',
                          style: theme.textTheme.labelLarge
                              ?.copyWith(fontWeight: FontWeight.bold),
                        ),
                      ),
                      _QuantityControls(
                        quantity: qty,
                        onChanged: (newQty) {
                          setState(() {
                            if (newQty > 0) {
                              _selectedServices[service.id] = newQty;
                            } else {
                              _selectedServices.remove(service.id);
                            }
                          });
                        },
                      ),
                    ],
                  ),
                );
              }),
              const Divider(height: 24),
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text('Grand Total',
                      style: theme.textTheme.titleMedium
                          ?.copyWith(fontWeight: FontWeight.bold)),
                  Text(
                    '\$${grandTotal.toStringAsFixed(2)}',
                    style: theme.textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.bold,
                      color: AppTheme.secondaryColor,
                    ),
                  ),
                ],
              ),
            ],
          ],
        ),
      ),
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
            _error.isNotEmpty ? _error : 'Something went wrong',
            textAlign: TextAlign.center,
            style: theme.textTheme.bodyLarge,
          ),
          const SizedBox(height: 24),
          ElevatedButton(
            onPressed: _loadData,
            child: const Text('Retry'),
          ),
        ],
      ),
    );
  }

  Widget _buildForm(ThemeData theme) {
    return Form(
      key: _formKey,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          // Addresses
          Text('Addresses', style: theme.textTheme.titleMedium),
          const SizedBox(height: 8),
          Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: _addresses.isEmpty
                    ? Column(
                        children: [
                          Icon(Icons.location_off_outlined,
                              size: 48, color: theme.colorScheme.outline),
                          const SizedBox(height: 12),
                          Text('No addresses saved yet',
                              style: theme.textTheme.titleMedium),
                          const SizedBox(height: 8),
                          TextButton.icon(
                            onPressed: () => context.push(AppRoutes.addAddress),
                            icon: const Icon(Icons.add),
                            label: const Text('Add an address'),
                          ),
                        ],
                      )
                    : Column(
                        children: [
                          DropdownButtonFormField<int>(
                    initialValue: _pickupAddress?.id,
                    decoration: const InputDecoration(
                      labelText: 'Pickup Address',
                      prefixIcon: Icon(Icons.local_shipping_outlined),
                    ),
                    items: _addresses
                        .map((a) => DropdownMenuItem(
                              value: a.id,
                              child: Text('${a.label} - ${a.street}'),
                            ))
                        .toList(),
                    onChanged: (value) {
                      setState(() {
                        _pickupAddress =
                            _addresses.firstWhere((a) => a.id == value);
                      });
                    },
                    validator: (value) =>
                        value == null ? 'Please select a pickup address' : null,
                  ),
                  const SizedBox(height: 16),
                  DropdownButtonFormField<int>(
                    initialValue: _deliveryAddress?.id,
                    decoration: const InputDecoration(
                      labelText: 'Delivery Address',
                      prefixIcon: Icon(Icons.home_outlined),
                    ),
                    items: _addresses
                        .map((a) => DropdownMenuItem(
                              value: a.id,
                              child: Text('${a.label} - ${a.street}'),
                            ))
                        .toList(),
                    onChanged: (value) {
                      setState(() {
                        _deliveryAddress =
                            _addresses.firstWhere((a) => a.id == value);
                      });
                    },
                    validator: (value) =>
                        value == null ? 'Please select a delivery address' : null,
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),

          // Schedule
          Text('Schedule', style: theme.textTheme.titleMedium),
          const SizedBox(height: 8),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                children: [
                  ListTile(
                    leading: const Icon(Icons.calendar_today),
                    title: const Text('Pickup Date'),
                    subtitle: Text(_formatDate(_pickupDate)),
                    onTap: _selectPickupDate,
                  ),
                  const Divider(),
                  ListTile(
                    leading: const Icon(Icons.calendar_today),
                    title: const Text('Delivery Date'),
                    subtitle: Text(_formatDate(_deliveryDate)),
                    onTap: _selectDeliveryDate,
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),

          // Services
          Text('SERVICES', style: theme.textTheme.titleMedium),
          const SizedBox(height: 8),

          // Search field
          TextField(
            controller: _searchController,
            decoration: InputDecoration(
              hintText: 'Search service...',
              prefixIcon: const Icon(Icons.search),
              suffixIcon: _searchQuery.isNotEmpty
                  ? IconButton(
                      icon: const Icon(Icons.clear),
                      onPressed: () {
                        _searchController.clear();
                        setState(() => _searchQuery = '');
                      },
                    )
                  : null,
            ),
            onChanged: (value) => setState(() => _searchQuery = value),
          ),
          const SizedBox(height: 8),

          // Category filter
          DropdownButtonFormField<int>(
            initialValue: _selectedCategory?.id,
            isExpanded: true,
            decoration: const InputDecoration(
              labelText: 'All Services',
              prefixIcon: Icon(Icons.category_outlined),
            ),
            items: [
              const DropdownMenuItem<int>(
                value: null,
                child: Text('All Services'),
              ),
              ..._categories.map((c) => DropdownMenuItem(
                    value: c.id,
                    child: Text(c.name),
                  )),
            ],
            onChanged: (value) {
              setState(() {
                _selectedCategory =
                    value == null ? null : _categories.firstWhere((c) => c.id == value);
              });
            },
          ),
          const SizedBox(height: 12),

          // Available services list (scrollable, bounded height, efficient)
          Text('Available Services', style: theme.textTheme.titleSmall),
          const SizedBox(height: 4),
          Container(
            constraints: const BoxConstraints(maxHeight: 420),
            decoration: BoxDecoration(
              color: theme.colorScheme.surface,
              borderRadius: BorderRadius.circular(12),
              border: Border.all(color: theme.dividerColor),
            ),
            child: _filteredServices.isEmpty
                ? const Padding(
                    padding: EdgeInsets.all(24),
                    child: Center(child: Text('No services found')),
                  )
                : ListView.builder(
                    shrinkWrap: true,
                    itemCount: _filteredServices.length,
                    itemBuilder: (context, index) {
                      final service = _filteredServices[index];
                      return _ServiceTile(
                        service: service,
                        quantity: _selectedServices[service.id] ?? 0,
                        onQuantityChanged: (qty) {
                          setState(() {
                            if (qty > 0) {
                              _selectedServices[service.id] = qty;
                            } else {
                              _selectedServices.remove(service.id);
                            }
                          });
                        },
                      );
                    },
                  ),
          ),
          const SizedBox(height: 16),

          // Selected Services / cart panel
          _buildCartPanel(theme),
          const SizedBox(height: 16),

          // Payment method
          Text('Payment Method', style: theme.textTheme.titleMedium),
          const SizedBox(height: 8),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: RadioGroup<PaymentMethod>(
                groupValue: _paymentMethod,
                onChanged: (value) {
                  setState(() => _paymentMethod = value!);
                },
                child: Column(
                  children: PaymentMethod.values
                      .where((m) => m != PaymentMethod.unknown)
                      .map((method) => RadioListTile<PaymentMethod>(
                            title: Text(method.displayName),
                            value: method,
                          ))
                      .toList(),
                ),
              ),
            ),
          ),
          const SizedBox(height: 16),

          // Coupon
          TextFormField(
            controller: _couponController,
            decoration: const InputDecoration(
              labelText: 'Coupon Code (optional)',
              prefixIcon: Icon(Icons.discount_outlined),
            ),
          ),
          const SizedBox(height: 16),

          // Special instructions
          TextFormField(
            controller: _specialInstructionsController,
            maxLines: 3,
            decoration: const InputDecoration(
              labelText: 'Special Instructions (optional)',
              prefixIcon: Icon(Icons.note_outlined),
              alignLabelWithHint: true,
            ),
          ),
          const SizedBox(height: 16),

          // Price summary
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                children: [
                  _PriceRow(label: 'Subtotal', value: _subtotal),
                  const Divider(height: 24),
                  _PriceRow(label: 'Total', value: _total, isBold: true),
                ],
              ),
            ),
          ),
          const SizedBox(height: 24),

          // Create button
          ElevatedButton(
            onPressed: _isLoading ? null : _createOrder,
            child: _isLoading
                ? const SizedBox(
                    width: 24,
                    height: 24,
                    child: CircularProgressIndicator(
                      color: Colors.white,
                      strokeWidth: 2,
                    ),
                  )
                : const Text('Place Order'),
          ),
          const SizedBox(height: 32),
        ],
      ),
    );
  }

  String _formatDate(DateTime date) {
    return '${date.month}/${date.day}/${date.year}';
  }
}

class _ServiceTile extends StatelessWidget {
  final ServiceModel service;
  final int quantity;
  final ValueChanged<int> onQuantityChanged;

  const _ServiceTile({
    required this.service,
    required this.quantity,
    required this.onQuantityChanged,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final unitType = service.pricePerKg != null ? 'per kg' : 'per piece';
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  service.name,
                  style: theme.textTheme.labelLarge,
                ),
                const SizedBox(height: 2),
                Text(
                  '\$${service.pricePerPiece.toStringAsFixed(2)} $unitType',
                  style: theme.textTheme.bodySmall,
                ),
              ],
            ),
          ),
          _QuantityControls(
            quantity: quantity,
            onChanged: onQuantityChanged,
          ),
        ],
      ),
    );
  }
}

/// Compact − qty + controls used in both the services list and the cart.
class _QuantityControls extends StatelessWidget {
  final int quantity;
  final ValueChanged<int> onChanged;

  const _QuantityControls({
    required this.quantity,
    required this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        IconButton(
          visualDensity: VisualDensity.compact,
          icon: const Icon(Icons.remove_circle_outline),
          onPressed: quantity > 0 ? () => onChanged(quantity - 1) : null,
        ),
        SizedBox(
          width: 28,
          child: Text(
            '$quantity',
            textAlign: TextAlign.center,
            style: theme.textTheme.titleMedium,
          ),
        ),
        IconButton(
          visualDensity: VisualDensity.compact,
          icon: const Icon(Icons.add_circle_outline),
          onPressed: () => onChanged(quantity + 1),
        ),
      ],
    );
  }
}

class _PriceRow extends StatelessWidget {
  final String label;
  final double value;
  final bool isBold;

  const _PriceRow({
    required this.label,
    required this.value,
    this.isBold = false,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final style = isBold
        ? theme.textTheme.titleMedium?.copyWith(
            fontWeight: FontWeight.bold,
            color: AppTheme.textPrimary,
          )
        : theme.textTheme.bodyMedium;

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