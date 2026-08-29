import 'package:flutter/material.dart';
import 'package:fluttertoast/fluttertoast.dart';
import 'package:go_router/go_router.dart';
import '../../core/data/models/dashboard_model.dart';
import '../../core/data/models/order_models.dart';
import '../../core/di/injection.dart';
import '../../core/theme/app_theme.dart';
import '../../core/usecases/order_usecases.dart';

class DriverAssignmentsScreen extends StatefulWidget {
  const DriverAssignmentsScreen({super.key});

  @override
  State<DriverAssignmentsScreen> createState() =>
      _DriverAssignmentsScreenState();
}

class _DriverAssignmentsScreenState extends State<DriverAssignmentsScreen> {
  List<DriverAssignmentDetailModel> _assignments = [];
  bool _isLoading = true;
  String? _error;
  bool _loadingMore = false;
  int _page = 1;
  bool _hasMore = true;

  @override
  void initState() {
    super.initState();
    _loadAssignments();
  }

  Future<void> _loadAssignments() async {
    setState(() {
      _isLoading = true;
      _error = null;
      _page = 1;
      _hasMore = true;
    });

    final result = await sl<GetDriverAssignmentsUseCase>()(
      const GetDriverAssignmentsParams(page: 1, pageSize: 20),
    );

    if (!mounted) return;

    result.fold(
      (failure) {
        setState(() {
          _isLoading = false;
          _error = failure.message;
        });
      },
      (assignments) {
        setState(() {
          _assignments = assignments;
          _isLoading = false;
          _hasMore = assignments.length == 20;
        });
      },
    );
  }

  Future<void> _loadMore() async {
    if (_loadingMore || !_hasMore) return;

    setState(() => _loadingMore = true);

    final result = await sl<GetDriverAssignmentsUseCase>()(
      GetDriverAssignmentsParams(page: _page + 1, pageSize: 20),
    );

    if (!mounted) return;

    result.fold(
      (failure) {
        setState(() => _loadingMore = false);
      },
      (assignments) {
        setState(() {
          _assignments = [..._assignments, ...assignments];
          _page++;
          _hasMore = assignments.length == 20;
          _loadingMore = false;
        });
      },
    );
  }

  Future<void> _acceptAssignment(int assignmentId) async {
    final result = await sl<AcceptDriverAssignmentUseCase>()(assignmentId);

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
        Fluttertoast.showToast(
          msg: 'Assignment accepted!',
          toastLength: Toast.LENGTH_SHORT,
          gravity: ToastGravity.BOTTOM,
          backgroundColor: AppTheme.secondaryColor,
          textColor: Colors.white,
        );
        _loadAssignments();
      },
    );
  }

  Future<void> _notifyOnTheWay(int assignmentId) async {
    final result = await sl<NotifyOnTheWayUseCase>()(assignmentId);

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
        Fluttertoast.showToast(
          msg: 'Customer notified: you are on the way!',
          toastLength: Toast.LENGTH_SHORT,
          gravity: ToastGravity.BOTTOM,
          backgroundColor: AppTheme.secondaryColor,
          textColor: Colors.white,
        );
      },
    );
  }

  Future<void> _notifyAtGate(int assignmentId) async {
    final result = await sl<NotifyAtGateUseCase>()(assignmentId);

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
        Fluttertoast.showToast(
          msg: 'Customer notified: you are at the gate!',
          toastLength: Toast.LENGTH_SHORT,
          gravity: ToastGravity.BOTTOM,
          backgroundColor: AppTheme.secondaryColor,
          textColor: Colors.white,
        );
      },
    );
  }

  Future<void> _completeAssignment(int assignmentId) async {
    final result = await sl<CompleteDriverAssignmentUseCase>()(assignmentId);

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
        Fluttertoast.showToast(
          msg: 'Assignment completed!',
          toastLength: Toast.LENGTH_SHORT,
          gravity: ToastGravity.BOTTOM,
          backgroundColor: AppTheme.secondaryColor,
          textColor: Colors.white,
        );
        _loadAssignments();
      },
    );
  }

  Future<void> _collectPayment(int assignmentId) async {
    final result = await sl<CollectPaymentUseCase>()(assignmentId);

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
        Fluttertoast.showToast(
          msg: 'Payment collected!',
          toastLength: Toast.LENGTH_SHORT,
          gravity: ToastGravity.BOTTOM,
          backgroundColor: AppTheme.secondaryColor,
          textColor: Colors.white,
        );
        _loadAssignments();
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      backgroundColor: theme.colorScheme.surface,
      appBar: AppBar(
        title: const Text('My Assignments'),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.pop(),
        ),
      ),
      body: RefreshIndicator(
        onRefresh: _loadAssignments,
        child: _isLoading
            ? const Center(child: CircularProgressIndicator())
            : _error != null
                ? _buildError(theme)
                : _assignments.isEmpty
                    ? _buildEmpty(theme)
                    : _buildAssignmentsList(theme),
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
            onPressed: _loadAssignments,
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
          Icons.assignment_outlined,
          size: 64,
          color: theme.colorScheme.outline,
        ),
        const SizedBox(height: 16),
        Text(
          'No assignments yet',
          textAlign: TextAlign.center,
          style: theme.textTheme.titleLarge,
        ),
        const SizedBox(height: 8),
        Text(
          'Your job assignments will appear here',
          textAlign: TextAlign.center,
          style: theme.textTheme.bodyMedium,
        ),
      ],
    );
  }

  Widget _buildAssignmentsList(ThemeData theme) {
    return ListView.builder(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.all(16),
      itemCount: _assignments.length + (_hasMore ? 1 : 0),
      itemBuilder: (context, index) {
        if (index >= _assignments.length) {
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

        final assignment = _assignments[index];
        return _AssignmentCard(
          assignment: assignment,
          onAccept: () => _acceptAssignment(assignment.id),
          onNotifyOnTheWay: () => _notifyOnTheWay(assignment.id),
          onNotifyAtGate: () => _notifyAtGate(assignment.id),
          onComplete: () => _completeAssignment(assignment.id),
          onCollectPayment: () => _collectPayment(assignment.id),
        );
      },
    );
  }
}

class _AssignmentCard extends StatelessWidget {
  final DriverAssignmentDetailModel assignment;
  final VoidCallback onAccept;
  final VoidCallback onNotifyOnTheWay;
  final VoidCallback onNotifyAtGate;
  final VoidCallback onComplete;
  final VoidCallback onCollectPayment;

  const _AssignmentCard({
    required this.assignment,
    required this.onAccept,
    required this.onNotifyOnTheWay,
    required this.onNotifyAtGate,
    required this.onComplete,
    required this.onCollectPayment,
  });

  IconData _actionIcon() {
    switch (assignment.action) {
      case DriverJobAction.pending:
        return Icons.pending_actions_outlined;
      case DriverJobAction.accepted:
        return Icons.check_circle_outline;
      case DriverJobAction.rejected:
        return Icons.cancel_outlined;
      case DriverJobAction.completed:
        return Icons.done_all;
      default:
        return Icons.assignment_outlined;
    }
  }

  Color _actionColor() {
    switch (assignment.action) {
      case DriverJobAction.pending:
        return AppTheme.primaryColor;
      case DriverJobAction.accepted:
        return AppTheme.secondaryColor;
      case DriverJobAction.rejected:
        return AppTheme.errorColor;
      case DriverJobAction.completed:
        return AppTheme.textSecondary;
      default:
        return AppTheme.primaryColor;
    }
  }

  String _formatAddress(String? street, String? city) {
    final parts = <String>[];
    if (street != null && street.isNotEmpty) parts.add(street);
    if (city != null && city.isNotEmpty) parts.add(city);
    return parts.isEmpty ? 'N/A' : parts.join(', ');
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isPickup = assignment.isPickup;
    final paymentCollected = assignment.isPaymentCollected;

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                CircleAvatar(
                  backgroundColor: AppTheme.primaryColor.withValues(alpha: 0.1),
                  child: Icon(
                    isPickup ? Icons.local_taxi : Icons.home,
                    color: AppTheme.primaryColor,
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        assignment.orderNumber,
                        style: theme.textTheme.labelLarge,
                      ),
                      const SizedBox(height: 2),
                      Text(
                        isPickup ? 'Pickup Job' : 'Delivery Job',
                        style: theme.textTheme.bodySmall?.copyWith(
                          color: isPickup
                              ? AppTheme.accentColor
                              : AppTheme.secondaryColor,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ],
                  ),
                ),
                Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                  decoration: BoxDecoration(
                    color: _actionColor().withValues(alpha: 0.1),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Row(
                    children: [
                      Icon(_actionIcon(),
                          size: 14, color: _actionColor()),
                      const SizedBox(width: 4),
                      Text(
                        assignment.action.displayName,
                        style: theme.textTheme.bodySmall?.copyWith(
                          color: _actionColor(),
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            if (assignment.customerName != null)
              Text(
                'Customer: ${assignment.customerName}',
                style: theme.textTheme.bodyMedium,
              ),
            if (assignment.customerPhone != null)
              Text(
                'Phone: ${assignment.customerPhone}',
                style: theme.textTheme.bodySmall,
              ),
            const SizedBox(height: 8),
            Row(
              children: [
                Expanded(
                  child: _LocationRow(
                    icon: Icons.location_on_outlined,
                    label: 'Pickup',
                    address: _formatAddress(
                        assignment.pickupStreet, assignment.pickupCityName),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: _LocationRow(
                    icon: Icons.home_outlined,
                    label: 'Delivery',
                    address: _formatAddress(assignment.deliveryStreet,
                        assignment.deliveryCityName),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 8),
            if (assignment.serviceNames.isNotEmpty)
              Wrap(
                spacing: 6,
                children: assignment.serviceNames
                    .map((s) => Chip(
                          label: Text(s),
                          backgroundColor:
                              AppTheme.primaryColor.withValues(alpha: 0.1),
                        ))
                    .toList(),
              ),
            const SizedBox(height: 8),
            if (!isPickup)
              Text(
                paymentCollected
                    ? 'Payment: ${assignment.paymentMethod.displayName} - COLLECTED'
                    : 'Payment: ${assignment.paymentMethod.displayName} - ${assignment.paymentStatus.displayName}',
                style: theme.textTheme.bodySmall?.copyWith(
                  color: paymentCollected
                      ? AppTheme.secondaryColor
                      : theme.colorScheme.error,
                  fontWeight: FontWeight.w600,
                ),
              ),
            const SizedBox(height: 12),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  '\$${assignment.totalAmount.toStringAsFixed(2)}',
                  style: theme.textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.bold,
                    color: AppTheme.primaryColor,
                  ),
                ),
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: [
                    if (assignment.canAccept)
                      ElevatedButton.icon(
                        onPressed: onAccept,
                        icon: const Icon(Icons.check, size: 16),
                        label: const Text('Accept'),
                      ),
                    if (assignment.action == DriverJobAction.accepted) ...[
                      ElevatedButton.icon(
                        onPressed: onNotifyOnTheWay,
                        icon: const Icon(Icons.directions_car, size: 16),
                        label: const Text('On the Way'),
                      ),
                      ElevatedButton.icon(
                        onPressed: onNotifyAtGate,
                        icon: const Icon(Icons.home_work_outlined, size: 16),
                        label: const Text('At Gate'),
                        style: ElevatedButton.styleFrom(
                          backgroundColor: AppTheme.accentColor,
                        ),
                      ),
                    ],
                    if (assignment.canComplete)
                      ElevatedButton.icon(
                        onPressed: onComplete,
                        icon: const Icon(Icons.done, size: 16),
                        label: const Text('Complete'),
                        style: ElevatedButton.styleFrom(
                          backgroundColor: AppTheme.secondaryColor,
                        ),
                      ),
                    if (assignment.canCollectPayment)
                      ElevatedButton.icon(
                        onPressed: onCollectPayment,
                        icon: const Icon(Icons.payments, size: 16),
                        label: const Text('Collect Payment'),
                        style: ElevatedButton.styleFrom(
                          backgroundColor: AppTheme.accentColor,
                        ),
                      ),
                  ],
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _LocationRow extends StatelessWidget {
  final IconData icon;
  final String label;
  final String address;

  const _LocationRow({
    required this.icon,
    required this.label,
    required this.address,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Row(
      children: [
        Icon(icon, size: 16, color: AppTheme.textSecondary),
        const SizedBox(width: 4),
        Text(
          '$label: ',
          style: theme.textTheme.bodySmall,
        ),
        Expanded(
          child: Text(
            address,
            style: theme.textTheme.bodySmall,
            overflow: TextOverflow.ellipsis,
          ),
        ),
      ],
    );
  }
}