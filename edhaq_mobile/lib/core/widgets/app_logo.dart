import 'package:flutter/material.dart';
import '../theme/app_theme.dart';

/// Reusable eDhaq logo widget that prominently features the brand
/// primary colour.
///
/// Renders the **white** logo silhouette (transparent background) on
/// [backgroundColor], which defaults to [AppTheme.primaryColor] so the
/// brand red is behind it and the white mark pops.  Pass a transparent
/// background when the logo sits directly on a red surface (splash).
class AppLogo extends StatelessWidget {
  final double size;
  final Color backgroundColor;
  final bool showBrandBorder;
  final double borderRadius;

  const AppLogo({
    super.key,
    this.size = 100,
    this.backgroundColor = AppTheme.primaryColor,
    this.showBrandBorder = false,
    this.borderRadius = 20,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      width: size,
      height: size,
      alignment: Alignment.center,
      decoration: BoxDecoration(
        color: backgroundColor,
        borderRadius: BorderRadius.circular(borderRadius),
        border: showBrandBorder
            ? Border.all(color: AppTheme.primaryColor, width: 3)
            : null,
      ),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(borderRadius > 4 ? borderRadius - 4 : 0),
        child: Image.asset(
          'assets/images/logo_white.png',
          fit: BoxFit.contain,
          errorBuilder: (_, _, _) => Icon(
            Icons.local_laundry_service,
            size: size * 0.5,
            color: Colors.white,
          ),
        ),
      ),
    );
  }
}
