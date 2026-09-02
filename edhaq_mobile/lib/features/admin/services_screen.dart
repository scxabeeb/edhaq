import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../../core/data/models/service_models.dart';
import '../../core/di/injection.dart';
import '../../core/network/api_service.dart';
import '../../core/theme/app_theme.dart';

/// Admin screen listing service categories and their services from the
/// backend (read-only view of the same data managed in the web admin).
class AdminServicesScreen extends StatefulWidget {
  const AdminServicesScreen({super.key});

  @override
  State<AdminServicesScreen> createState() => _AdminServicesScreenState();
}

class _AdminServicesScreenState extends State<AdminServicesScreen> {
  List<ServiceCategoryModel> _categories = [];
  Map<int, List<ServiceModel>> _servicesByCategory = {};
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadServices();
  }

  Future<void> _loadServices() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });

    try {
      final api = sl<ApiService>();
      final categories = await api.getServiceCategories();
      final services = await api.getServices();

      final byCategory = <int, List<ServiceModel>>{};
      for (final service in services) {
        byCategory.putIfAbsent(service.categoryId, () => []).add(service);
      }

      if (!mounted) return;
      setState(() {
        _categories = categories;
        _servicesByCategory = byCategory;
        _isLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _isLoading = false;
        _error = 'Failed to load services: $e';
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      backgroundColor: theme.colorScheme.surface,
      appBar: AppBar(
        title: const Text('Services'),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.pop(),
        ),
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? _buildError(theme)
              : RefreshIndicator(
                  onRefresh: _loadServices,
                  child: ListView(
                    physics: const AlwaysScrollableScrollPhysics(),
                    padding: const EdgeInsets.all(16),
                    children: [
                      for (final category in _categories) ...[
                        Row(
                          children: [
                            Icon(
                              Icons.category_outlined,
                              size: 20,
                              color: AppTheme.primaryColor,
                            ),
                            const SizedBox(width: 8),
                            Expanded(
                              child: Text(category.name,
                                  style: theme.textTheme.titleMedium),
                            ),
                            if (!category.isActive)
                              _chip(theme, 'Inactive',
                                  theme.colorScheme.outline),
                          ],
                        ),
                        if (category.description != null &&
                            category.description!.isNotEmpty)
                          Padding(
                            padding: const EdgeInsets.only(left: 28, top: 2),
                            child: Text(category.description!,
                                style: theme.textTheme.bodySmall),
                          ),
                        const SizedBox(height: 8),
                        ...?_servicesByCategory[category.id]
                            ?.map((s) => _ServiceCard(service: s)),
                        const SizedBox(height: 16),
                      ],
                    ],
                  ),
                ),
    );
  }

  Widget _chip(ThemeData theme, String label, Color color) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.1),
        borderRadius: BorderRadius.circular(10),
      ),
      child: Text(label,
          style: TextStyle(
              color: color, fontSize: 11, fontWeight: FontWeight.w600)),
    );
  }

  Widget _buildError(ThemeData theme) {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(Icons.error_outline, size: 64, color: theme.colorScheme.error),
          const SizedBox(height: 16),
          Text(_error ?? 'Something went wrong', textAlign: TextAlign.center),
          const SizedBox(height: 24),
          ElevatedButton(onPressed: _loadServices, child: const Text('Retry')),
        ],
      ),
    );
  }
}

class _ServiceCard extends StatelessWidget {
  final ServiceModel service;

  const _ServiceCard({required this.service});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    final priceParts = <String>[
      if (service.pricePerPiece > 0) '\$${service.pricePerPiece.toStringAsFixed(2)}/pc',
      if (service.pricePerKg != null && service.pricePerKg! > 0)
        '\$${service.pricePerKg!.toStringAsFixed(2)}/kg',
    ];

    final subtitle = [
      if (priceParts.isNotEmpty) priceParts.join('  •  '),
      '~${service.estimatedHours}h',
      if (service.description != null && service.description!.isNotEmpty)
        service.description!,
    ].join('\n');

    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: ListTile(
        leading: CircleAvatar(
          backgroundColor: AppTheme.primaryColor.withValues(alpha: 0.1),
          child: Icon(
            service.isExpress ? Icons.flash_on : Icons.local_laundry_service,
            size: 20,
            color: AppTheme.primaryColor,
          ),
        ),
        title: Text(service.name, style: theme.textTheme.titleSmall),
        subtitle: Text(subtitle, style: theme.textTheme.bodySmall),
        isThreeLine:
            service.description != null && service.description!.isNotEmpty,
        trailing: service.isExpress
            ? Container(
                padding:
                    const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                decoration: BoxDecoration(
                  color: Colors.amber.shade700.withValues(alpha: 0.12),
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Text('Express',
                    style: TextStyle(
                        color: Colors.amber.shade800,
                        fontSize: 11,
                        fontWeight: FontWeight.w600)),
              )
            : null,
      ),
    );
  }
}
