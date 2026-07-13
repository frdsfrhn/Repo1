import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'core/theme.dart';
import 'providers/auth_provider.dart';
import 'providers/lead_provider.dart';
import 'screens/auth/consent_screen.dart';
import 'screens/auth/phone_login_screen.dart';
import 'screens/dashboard/dashboard_screen.dart';
import 'screens/splash_screen.dart';
import 'services/firestore_service.dart';
import 'services/storage_service.dart';

/// Routes between login → PDPA consent → dashboard based on live auth and
/// profile state, and owns `MaterialApp` itself (not just its `home`).
///
/// `LeadProvider` is only meaningful once signed in, but it must wrap
/// `MaterialApp` — not just the dashboard screen — because `Navigator.push`
/// mounts pushed routes (lead detail, lead form) as siblings of the route
/// that pushed them, not as its descendants. A provider scoped around only
/// the first route's content is invisible to anything pushed on top of it;
/// wrapping the whole `MaterialApp` puts the provider above the Navigator,
/// so every route — however many are pushed — can see it.
class PropertyAgentApp extends StatelessWidget {
  const PropertyAgentApp({super.key});

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AppAuthProvider>();

    if (auth.isLoading) {
      return _shell(const SplashScreen());
    }
    if (!auth.isSignedIn) {
      return _shell(const PhoneLoginScreen());
    }
    if (!auth.hasGivenConsent) {
      return _shell(const ConsentScreen());
    }

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
      child: _shell(const DashboardScreen()),
    );
  }

  Widget _shell(Widget home) {
    return MaterialApp(
      title: 'PropertyMate',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light(),
      home: home,
    );
  }
}
