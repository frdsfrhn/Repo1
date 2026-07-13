import 'package:flutter/material.dart';

import '../core/theme.dart';

/// The source logo (assets/branding/logo.png) is a flattened image with its
/// own light canvas — not transparent — so the splash background is matched
/// to that canvas color rather than the app's primary green, to avoid a
/// visible seam around the artwork.
class SplashScreen extends StatelessWidget {
  const SplashScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return const Scaffold(
      backgroundColor: Color(0xFFF8F8F6),
      body: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Image(
              image: AssetImage('assets/branding/logo.png'),
              width: 260,
            ),
            SizedBox(height: 32),
            CircularProgressIndicator(color: AppTheme.primary),
          ],
        ),
      ),
    );
  }
}
