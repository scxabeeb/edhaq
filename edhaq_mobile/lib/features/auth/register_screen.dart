import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:fluttertoast/fluttertoast.dart';
import 'package:go_router/go_router.dart';
import '../../core/constants/app_constants.dart';
import '../../core/data/local/secure_storage_service.dart';
import '../../core/data/models/auth_request_models.dart';
import '../../core/data/models/location_models.dart';
import '../../core/di/injection.dart';
import '../../core/router/app_router.dart';
import '../../core/usecases/auth_usecases.dart';
import '../../core/usecases/location_usecases.dart';
import '../../core/usecases/usecase.dart';

class RegisterScreen extends StatefulWidget {
  const RegisterScreen({super.key});

  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends State<RegisterScreen> {
  final _formKey = GlobalKey<FormState>();
  final _firstNameController = TextEditingController();
  final _lastNameController = TextEditingController();
  final _emailController = TextEditingController();
  final _phoneController = TextEditingController();
  final _streetController = TextEditingController();
  final _districtController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();

  bool _obscurePassword = true;
  bool _obscureConfirmPassword = true;
  bool _isLoading = false;

  // Location data
  List<CityModel> _cities = [];
  List<VillageModel> _villages = [];
  List<SubVillageModel> _subVillages = [];
  CityModel? _selectedCity;
  VillageModel? _selectedVillage;
  SubVillageModel? _selectedSubVillage;
  bool _loadingLocations = false;

  @override
  void initState() {
    super.initState();
    _loadCities();
  }

  @override
  void dispose() {
    _firstNameController.dispose();
    _lastNameController.dispose();
    _emailController.dispose();
    _phoneController.dispose();
    _streetController.dispose();
    _districtController.dispose();
    _passwordController.dispose();
    _confirmPasswordController.dispose();
    super.dispose();
  }

  Future<void> _loadCities() async {
    setState(() => _loadingLocations = true);
    final result = await sl<GetCitiesUseCase>()(const NoParams());
    if (!mounted) return;
    result.fold(
      (failure) {
        Fluttertoast.showToast(
          msg: 'Failed to load cities: ${failure.message}',
          toastLength: Toast.LENGTH_LONG,
          gravity: ToastGravity.BOTTOM,
          backgroundColor: Colors.red.shade700,
          textColor: Colors.white,
        );
      },
      (cities) => setState(() => _cities = cities),
    );
    setState(() => _loadingLocations = false);
  }

  Future<void> _loadVillages(int cityId) async {
    setState(() => _loadingLocations = true);
    final result = await sl<GetVillagesUseCase>()(cityId);
    if (!mounted) return;
    result.fold(
      (failure) {
        Fluttertoast.showToast(
          msg: 'Failed to load villages: ${failure.message}',
          toastLength: Toast.LENGTH_LONG,
          gravity: ToastGravity.BOTTOM,
          backgroundColor: Colors.red.shade700,
          textColor: Colors.white,
        );
      },
      (villages) => setState(() {
        _villages = villages;
        _selectedVillage = null;
        _subVillages = [];
        _selectedSubVillage = null;
      }),
    );
    setState(() => _loadingLocations = false);
  }

  Future<void> _loadSubVillages(int villageId) async {
    setState(() => _loadingLocations = true);
    final result = await sl<GetSubVillagesUseCase>()(villageId);
    if (!mounted) return;
    result.fold(
      (failure) {
        Fluttertoast.showToast(
          msg: 'Failed to load sub-villages: ${failure.message}',
          toastLength: Toast.LENGTH_LONG,
          gravity: ToastGravity.BOTTOM,
          backgroundColor: Colors.red.shade700,
          textColor: Colors.white,
        );
      },
      (subVillages) => setState(() {
        _subVillages = subVillages;
        _selectedSubVillage = null;
      }),
    );
    setState(() => _loadingLocations = false);
  }

  Future<void> _register() async {
    if (!_formKey.currentState!.validate()) return;
    if (_selectedCity == null || _selectedVillage == null) {
      Fluttertoast.showToast(
        msg: 'Please select your city and village',
        toastLength: Toast.LENGTH_LONG,
        gravity: ToastGravity.BOTTOM,
        backgroundColor: Colors.red.shade700,
        textColor: Colors.white,
      );
      return;
    }

    setState(() => _isLoading = true);

    final request = RegisterRequest(
      firstName: _firstNameController.text.trim(),
      lastName: _lastNameController.text.trim(),
      email: _emailController.text.trim(),
      phoneNumber: _phoneController.text.trim(),
      password: _passwordController.text,
      confirmPassword: _confirmPasswordController.text,
      cityId: _selectedCity!.id,
      villageId: _selectedVillage!.id,
      subVillageId: _selectedSubVillage?.id,
      street: _streetController.text.trim(),
      district: _districtController.text.trim().isEmpty
          ? null
          : _districtController.text.trim(),
    );

    final result = await sl<RegisterUseCase>()(request);

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
      (response) async {
        final storage = sl<SecureStorageService>();
        await storage.write(AppConstants.authTokenKey, response.token);
        await storage.write(
          AppConstants.userKey,
          jsonEncode(response.user.toJson()),
        );

        setState(() => _isLoading = false);

        if (!mounted) return;
        context.go(AppRoutes.splash);
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      backgroundColor: theme.colorScheme.surface,
      appBar: AppBar(
        title: const Text('Create Account'),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.pop(),
        ),
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // Name fields
                Row(
                  children: [
                    Expanded(
                      child: TextFormField(
                        controller: _firstNameController,
                        textInputAction: TextInputAction.next,
                        decoration: const InputDecoration(
                          labelText: 'First Name',
                          prefixIcon: Icon(Icons.person_outline),
                        ),
                        validator: (value) {
                          if (value == null || value.trim().isEmpty) {
                            return 'Required';
                          }
                          return null;
                        },
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: TextFormField(
                        controller: _lastNameController,
                        textInputAction: TextInputAction.next,
                        decoration: const InputDecoration(
                          labelText: 'Last Name',
                          prefixIcon: Icon(Icons.person_outline),
                        ),
                        validator: (value) {
                          if (value == null || value.trim().isEmpty) {
                            return 'Required';
                          }
                          return null;
                        },
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 16),

                // Email
                TextFormField(
                  controller: _emailController,
                  keyboardType: TextInputType.emailAddress,
                  textInputAction: TextInputAction.next,
                  decoration: const InputDecoration(
                    labelText: 'Email',
                    hintText: 'you@example.com',
                    prefixIcon: Icon(Icons.email_outlined),
                  ),
                  validator: (value) {
                    if (value == null || value.trim().isEmpty) {
                      return 'Email is required';
                    }
                    if (!value.contains('@')) {
                      return 'Enter a valid email';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 16),

                // Phone
                TextFormField(
                  controller: _phoneController,
                  keyboardType: TextInputType.phone,
                  textInputAction: TextInputAction.next,
                  decoration: const InputDecoration(
                    labelText: 'Phone Number',
                    hintText: '+252...',
                    prefixIcon: Icon(Icons.phone_outlined),
                  ),
                  validator: (value) {
                    if (value == null || value.trim().isEmpty) {
                      return 'Phone number is required';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 16),

                // City dropdown
                DropdownButtonFormField<int>(
                  initialValue: _selectedCity?.id,
                  decoration: const InputDecoration(
                    labelText: 'City',
                    prefixIcon: Icon(Icons.location_city_outlined),
                  ),
                  items: _cities
                      .map((c) => DropdownMenuItem(
                            value: c.id,
                            child: Text(c.name),
                          ))
                      .toList(),
                  onChanged: _loadingLocations
                      ? null
                      : (value) {
                          final city = _cities.firstWhere((c) => c.id == value);
                          setState(() => _selectedCity = city);
                          _loadVillages(city.id);
                        },
                  validator: (value) =>
                      value == null ? 'Please select a city' : null,
                ),
                const SizedBox(height: 16),

                // Village dropdown
                DropdownButtonFormField<int>(
                  initialValue: _selectedVillage?.id,
                  decoration: const InputDecoration(
                    labelText: 'Village',
                    prefixIcon: Icon(Icons.location_on_outlined),
                  ),
                  items: _villages
                      .map((v) => DropdownMenuItem(
                            value: v.id,
                            child: Text(v.name),
                          ))
                      .toList(),
                  onChanged: _loadingLocations || _villages.isEmpty
                      ? null
                      : (value) {
                          final village =
                              _villages.firstWhere((v) => v.id == value);
                          setState(() => _selectedVillage = village);
                          _loadSubVillages(village.id);
                        },
                  validator: (value) =>
                      value == null ? 'Please select a village' : null,
                ),
                const SizedBox(height: 16),

                // Sub-village dropdown (optional)
                if (_subVillages.isNotEmpty) ...[
                  DropdownButtonFormField<int>(
                    initialValue: _selectedSubVillage?.id,
                    decoration: const InputDecoration(
                      labelText: 'Sub-Village (optional)',
                      prefixIcon: Icon(Icons.place_outlined),
                    ),
                    items: _subVillages
                        .map((s) => DropdownMenuItem(
                              value: s.id,
                              child: Text(s.name),
                            ))
                        .toList(),
                    onChanged: _loadingLocations
                        ? null
                        : (value) {
                            final subVillage =
                                _subVillages.firstWhere((s) => s.id == value);
                            setState(() => _selectedSubVillage = subVillage);
                          },
                  ),
                  const SizedBox(height: 16),
                ],

                // Street
                TextFormField(
                  controller: _streetController,
                  textInputAction: TextInputAction.next,
                  decoration: const InputDecoration(
                    labelText: 'Street Address',
                    hintText: 'e.g. 123 Main Street',
                    prefixIcon: Icon(Icons.home_outlined),
                  ),
                  validator: (value) {
                    if (value == null || value.trim().isEmpty) {
                      return 'Street address is required';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 16),

                // District (optional)
                TextFormField(
                  controller: _districtController,
                  textInputAction: TextInputAction.next,
                  decoration: const InputDecoration(
                    labelText: 'District (optional)',
                    prefixIcon: Icon(Icons.map_outlined),
                  ),
                ),
                const SizedBox(height: 16),

                // Password
                TextFormField(
                  controller: _passwordController,
                  obscureText: _obscurePassword,
                  textInputAction: TextInputAction.next,
                  decoration: InputDecoration(
                    labelText: 'Password',
                    hintText: 'At least 8 characters',
                    prefixIcon: const Icon(Icons.lock_outline),
                    suffixIcon: IconButton(
                      icon: Icon(
                        _obscurePassword
                            ? Icons.visibility_off
                            : Icons.visibility,
                      ),
                      onPressed: () =>
                          setState(() => _obscurePassword = !_obscurePassword),
                    ),
                  ),
                  validator: (value) {
                    if (value == null || value.isEmpty) {
                      return 'Password is required';
                    }
                    if (value.length < 8) {
                      return 'Password must be at least 8 characters';
                    }
                    if (!RegExp(r'[A-Z]').hasMatch(value)) {
                      return 'Must contain an uppercase letter';
                    }
                    if (!RegExp(r'[a-z]').hasMatch(value)) {
                      return 'Must contain a lowercase letter';
                    }
                    if (!RegExp(r'[0-9]').hasMatch(value)) {
                      return 'Must contain a number';
                    }
                    if (!RegExp(r'[^A-Za-z0-9]').hasMatch(value)) {
                      return 'Must contain a special character';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 16),

                // Confirm password
                TextFormField(
                  controller: _confirmPasswordController,
                  obscureText: _obscureConfirmPassword,
                  textInputAction: TextInputAction.done,
                  onFieldSubmitted: (_) => _register(),
                  decoration: InputDecoration(
                    labelText: 'Confirm Password',
                    prefixIcon: const Icon(Icons.lock_outline),
                    suffixIcon: IconButton(
                      icon: Icon(
                        _obscureConfirmPassword
                            ? Icons.visibility_off
                            : Icons.visibility,
                      ),
                      onPressed: () => setState(
                          () => _obscureConfirmPassword = !_obscureConfirmPassword),
                    ),
                  ),
                  validator: (value) {
                    if (value == null || value.isEmpty) {
                      return 'Please confirm your password';
                    }
                    if (value != _passwordController.text) {
                      return 'Passwords do not match';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 24),

                // Register button
                ElevatedButton(
                  onPressed: _isLoading ? null : _register,
                  child: _isLoading
                      ? const SizedBox(
                          width: 24,
                          height: 24,
                          child: CircularProgressIndicator(
                            color: Colors.white,
                            strokeWidth: 2,
                          ),
                        )
                      : const Text('Create Account'),
                ),
                const SizedBox(height: 16),

                // Login link
                Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Text(
                      'Already have an account? ',
                      style: theme.textTheme.bodyMedium,
                    ),
                    TextButton(
                      onPressed: () => context.push(AppRoutes.login),
                      child: const Text('Sign In'),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}