import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../../core/data/models/user_model.dart';
import '../../core/di/injection.dart';
import '../../core/network/api_service.dart';
import '../../core/theme/app_theme.dart';

/// Admin screen listing users from the backend, optionally filtered by role.
class AdminUsersScreen extends StatefulWidget {
  final String title;
  final String? role;

  const AdminUsersScreen({super.key, required this.title, this.role});

  @override
  State<AdminUsersScreen> createState() => _AdminUsersScreenState();
}

class _AdminUsersScreenState extends State<AdminUsersScreen> {
  List<AppUser> _users = [];
  bool _isLoading = true;
  String? _error;
  String _search = '';

  @override
  void initState() {
    super.initState();
    _loadUsers();
  }

  Future<void> _loadUsers() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });

    try {
      final users =
          await sl<ApiService>().getUsers(role: widget.role, search: _search);
      if (!mounted) return;
      setState(() {
        _users = users;
        _isLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _isLoading = false;
        _error = 'Failed to load users: $e';
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      backgroundColor: theme.colorScheme.surface,
      appBar: AppBar(
        title: Text(widget.title),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.pop(),
        ),
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
            child: TextField(
              decoration: InputDecoration(
                hintText: 'Search by name, email or phone',
                prefixIcon: const Icon(Icons.search),
                isDense: true,
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
              ),
              textInputAction: TextInputAction.search,
              onSubmitted: (value) {
                _search = value.trim();
                _loadUsers();
              },
            ),
          ),
          Expanded(
            child: _isLoading
                ? const Center(child: CircularProgressIndicator())
                : _error != null
                    ? _buildError(theme)
                    : _users.isEmpty
                        ? _buildEmpty(theme)
                        : RefreshIndicator(
                            onRefresh: _loadUsers,
                            child: ListView.separated(
                              physics: const AlwaysScrollableScrollPhysics(),
                              padding: const EdgeInsets.all(16),
                              itemCount: _users.length,
                              separatorBuilder: (_, _) =>
                                  const SizedBox(height: 8),
                              itemBuilder: (context, index) =>
                                  _UserCard(user: _users[index]),
                            ),
                          ),
          ),
        ],
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
          Text(_error ?? 'Something went wrong', textAlign: TextAlign.center),
          const SizedBox(height: 24),
          ElevatedButton(onPressed: _loadUsers, child: const Text('Retry')),
        ],
      ),
    );
  }

  Widget _buildEmpty(ThemeData theme) {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(Icons.people_outline, size: 64, color: theme.colorScheme.outline),
          const SizedBox(height: 12),
          Text('No users found', style: theme.textTheme.titleMedium),
        ],
      ),
    );
  }
}

class _UserCard extends StatelessWidget {
  final AppUser user;

  const _UserCard({required this.user});

  Color _roleColor(String role) {
    return switch (role) {
      'Administrator' => Colors.red.shade700,
      'Manager' => Colors.orange.shade800,
      'Cashier' => Colors.purple.shade700,
      'LaundryStaff' => Colors.teal.shade700,
      'PickupDriver' || 'DeliveryDriver' => Colors.blue.shade700,
      _ => AppTheme.primaryColor,
    };
  }

  String _roleLabel(String role) => switch (role) {
        'PickupDriver' => 'Pickup Driver',
        'DeliveryDriver' => 'Delivery Driver',
        'LaundryStaff' => 'Laundry Staff',
        _ => role,
      };

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Card(
      child: ListTile(
        leading: CircleAvatar(
          backgroundColor: AppTheme.primaryColor.withValues(alpha: 0.1),
          child: Text(
            user.firstName.isNotEmpty ? user.firstName[0].toUpperCase() : '?',
            style: const TextStyle(
                color: AppTheme.primaryColor, fontWeight: FontWeight.bold),
          ),
        ),
        title: Text(user.fullName.isEmpty ? user.email : user.fullName,
            style: theme.textTheme.titleSmall),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(user.email, style: theme.textTheme.bodySmall),
            if (user.phoneNumber != null && user.phoneNumber!.isNotEmpty)
              Text(user.phoneNumber!, style: theme.textTheme.bodySmall),
            const SizedBox(height: 4),
            Wrap(
              spacing: 6,
              runSpacing: 4,
              children: user.roles.map((role) {
                final color = _roleColor(role);
                return Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                  decoration: BoxDecoration(
                    color: color.withValues(alpha: 0.1),
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(color: color.withValues(alpha: 0.3)),
                  ),
                  child: Text(
                    _roleLabel(role),
                    style: TextStyle(
                      color: color,
                      fontSize: 11,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                );
              }).toList(),
            ),
          ],
        ),
      ),
    );
  }
}
