import 'package:flutter/material.dart';
import '../theme/app_theme.dart';

/// Reusable eDhaq logo widget that prominently features the brand
/// primary colour.
///
/// [backgroundColor] defaults to [AppTheme.primaryColor] so the brand
/// red (#D71920) is the dominant colour behind the logo.  Pass an
/// explicit white background (with the brand border) when the logo must
/// sit on a surface that is already the brand colour (e.g. splash).
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
        boxShadow: [
          BoxShadow(
            color: AppTheme.primaryColor.withValues(alpha: 0.3),
            blurRadius: 20,
            spreadRadius: 2,
          ),
        ],
      ),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(borderRadius - 4),
        child: Image.asset(
          'assets/images/logo.png',
          fit: BoxFit.contain,
        ),
      ),
    );
  }
}
