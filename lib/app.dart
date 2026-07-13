import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'core/theme.dart';
import 'providers/auth_provider.dart';
import 'providers/lead_provider.dart';
import 'providers/subscription_provider.dart';
import 'screens/auth/consent_screen.dart';
import 'screens/auth/phone_login_screen.dart';
import 'screens/dashboard/dashboard_screen.dart';
import 'screens/splash_screen.dart';
import 'services/firestore_service.dart';
import 'services/storage_service.dart';

class PropertyAgentApp extends StatelessWidget {
  const PropertyAgentApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Property Agent',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light(),
      home: const _AuthGate(),
    );
  }
}

/// Routes between login → PDPA consent → dashboard based on live auth and
/// profile state. Every branch here has a defined widget even with zero
/// data (splash while loading, login form, consent screen, empty
/// dashboard) — nothing renders a blank screen on first launch.
class _AuthGate extends StatelessWidget {
  const _AuthGate();

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();

    if (auth.isLoading) {
      return const SplashScreen();
    }
    if (!auth.isSignedIn) {
      return const PhoneLoginScreen();
    }
    if (!auth.hasGivenConsent) {
      return const ConsentScreen();
    }

    // Keep SubscriptionProvider (paywall/trial gating) in sync with the
    // agent's profile document as it changes.
    context.read<SubscriptionProvider>().updateProfile(auth.profile);

    return MultiProvider(
      key: ValueKey(auth.firebaseUser!.uid),
      providers: [
        ChangeNotifierProvider<LeadProvider>(
          create: (_) => LeadProvider(
            firestoreService: FirestoreService(),
            storageService: StorageService(),
            agentId: auth.firebaseUser!.uid,
          ),
        ),
      ],
      child: const DashboardScreen(),
    );
  }
}
