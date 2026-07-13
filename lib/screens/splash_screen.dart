import 'package:flutter/material.dart';

import '../core/theme.dart';

class SplashScreen extends StatelessWidget {
  const SplashScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppTheme.primary,
      body: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: 88,
              height: 88,
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(20),
              ),
              child: const Icon(Icons.home_work_rounded,
                  size: 48, color: AppTheme.primary),
            ),
            const SizedBox(height: 20),
            const Text(
              'PropertyMate',
              style: TextStyle(
                color: Colors.white,
                fontSize: 24,
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(height: 28),
            const CircularProgressIndicator(color: Colors.white),
          ],
        ),
      ),
    );
  }
}
