import 'package:flutter/material.dart';
import 'core/di/injection.dart';
import 'core/router/app_router.dart';
import 'core/services/notification_polling_service.dart';
import 'core/theme/app_theme.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await initDependencies();
  // Start polling for new tasks/notifications (driver & customer).
  // If nobody is logged in, the service backs off until the next launch.
  await notificationPolling.start();
  runApp(const EDhaqApp());
}

class EDhaqApp extends StatelessWidget {
  const EDhaqApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp.router(
      title: 'eDhaq Laundry',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light,
      routerConfig: appRouter,
    );
  }
}