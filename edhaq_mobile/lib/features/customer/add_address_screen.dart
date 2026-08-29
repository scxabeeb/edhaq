import 'package:flutter/material.dart';
import 'package:fluttertoast/fluttertoast.dart';
import 'package:go_router/go_router.dart';
import '../../core/data/models/address_models.dart';
import '../../core/data/models/location_models.dart';
import '../../core/di/injection.dart';
import '../../core/theme/app_theme.dart';
import '../../core/usecases/address_usecases.dart';
import '../../core/usecases/location_usecases.dart';
import '../../core/usecases/usecase.dart';

class AddAddressScreen extends StatefulWidget {
  const AddAddressScreen({super.key});

  @override
  State<AddAddressScreen> createState() => _AddAddressScreenState();
}

class _AddAddressScreenState extends State<AddAddressScreen> {
  final _formKey = GlobalKey<FormState>();
  final _labelController = TextEditingController(text: 'Home');
  final _streetController = TextEditingController();
  final _districtController = TextEditingController();

  bool _isLoading = false;
  bool _loadingLocations = true;
  String? _error;

  // Location data
  List<CityModel> _cities = [];
  List<VillageModel> _villages = [];
  List<SubVillageModel> _subVillages = [];
  CityModel? _selectedCity;
  VillageModel? _selectedVillage;
  SubVillageModel? _selectedSubVillage;

  bool _isDefault = false;

  @override
  void initState() {
    super.initState();
    _loadCities();
  }

  @override
  void dispose() {
    _labelController.dispose();
    _streetController.dispose();
    _districtController.dispose();
    super.dispose();
  }

  Future<void> _loadCities() async {
    setState(() => _loadingLocations = true);
    final result = await sl<GetCitiesUseCase>()(const NoParams());
    if (!mounted) return;
    result.fold(
      (failure) {
        setState(() => _error = failure.message);
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

  Future<void> _saveAddress() async {
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

    final request = CreateAddressRequest(
      label: _labelController.text.trim().isEmpty
          ? 'Home'
          : _labelController.text.trim(),
      street: _streetController.text.trim(),
      district: _districtController.text.trim().isEmpty
          ? null
          : _districtController.text.trim(),
      cityId: _selectedCity!.id,
      villageId: _selectedVillage!.id,
      subVillageId: _selectedSubVillage?.id,
      isDefault: _isDefault,
    );

    final result = await sl<CreateAddressUseCase>()(request);

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
      (_) {
        setState(() => _isLoading = false);
        Fluttertoast.showToast(
          msg: 'Address saved!',
          toastLength: Toast.LENGTH_SHORT,
          gravity: ToastGravity.BOTTOM,
          backgroundColor: AppTheme.secondaryColor,
          textColor: Colors.white,
        );
        context.pop();
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      backgroundColor: theme.colorScheme.surface,
      appBar: AppBar(
        title: const Text('Add Address'),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.pop(),
        ),
      ),
      body: _error != null
          ? _buildError(theme)
          : _buildForm(theme),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: _isLoading ? null : _saveAddress,
        icon: _isLoading
            ? const SizedBox(
                width: 24,
                height: 24,
                child: CircularProgressIndicator(
                  color: Colors.white,
                  strokeWidth: 2,
                ),
              )
            : const Icon(Icons.save),
        label: const Text('Save Address'),
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
            _error ?? 'Something went wrong',
            textAlign: TextAlign.center,
            style: theme.textTheme.bodyLarge,
          ),
          const SizedBox(height: 24),
          ElevatedButton(
            onPressed: _loadCities,
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
          TextFormField(
            controller: _labelController,
            decoration: const InputDecoration(
              labelText: 'Label',
              hintText: 'e.g. Home, Work',
              prefixIcon: Icon(Icons.label_outlined),
            ),
            validator: (value) {
              if (value == null || value.trim().isEmpty) {
                return 'Label is required';
              }
              return null;
            },
          ),
          const SizedBox(height: 16),
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
          TextFormField(
            controller: _districtController,
            textInputAction: TextInputAction.next,
            decoration: const InputDecoration(
              labelText: 'District (optional)',
              prefixIcon: Icon(Icons.map_outlined),
            ),
          ),
          const SizedBox(height: 16),
          SwitchListTile(
            title: const Text('Set as default'),
            value: _isDefault,
            onChanged: (value) => setState(() => _isDefault = value),
          ),
          const SizedBox(height: 16),
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
                    final village = _villages.firstWhere((v) => v.id == value);
                    setState(() => _selectedVillage = village);
                    _loadSubVillages(village.id);
                  },
            validator: (value) =>
                value == null ? 'Please select a village' : null,
          ),
          const SizedBox(height: 16),
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
        ],
      ),
    );
  }
}
