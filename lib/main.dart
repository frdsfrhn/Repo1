import 'package:firebase_core/firebase_core.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'app.dart';
import 'core/env.dart';
import 'firebase_options.dart';
import 'providers/auth_provider.dart';
import 'providers/subscription_provider.dart';
import 'services/auth_service.dart';
import 'services/subscription_service.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

  await Firebase.initializeApp(options: DefaultFirebaseOptions.currentPlatform);

  final subscriptionService = SubscriptionService();
  if (Env.revenueCatApiKey.isNotEmpty) {
    await subscriptionService.configure(apiKey: Env.revenueCatApiKey);
  } else {
    debugPrint(
      'RevenueCat API key missing — pass --dart-define=REVENUECAT_API_KEY_ANDROID=... '
      '(and/or _IOS) when running. Subscription screens will not work until set.',
    );
  }

  runApp(
    MultiProvider(
      providers: [
        ChangeNotifierProvider<AppAuthProvider>(
          create: (_) => AppAuthProvider(
            authService: AuthService(),
            subscriptionService: subscriptionService,
          ),
        ),
        // Derives SubscriptionProvider's state from AppAuthProvider's
        // profile stream. Using a proxy provider (rather than calling
        // updateProfile() from inside a widget's build() method) means the
        // update happens through Provider's own change-dispatch machinery,
        // not synchronously during another widget's build — which Flutter
        // disallows ("setState() or markNeedsBuild() called during build").
        ChangeNotifierProxyProvider<AppAuthProvider, SubscriptionProvider>(
          create: (_) => SubscriptionProvider(),
          update: (_, auth, subscriptionProvider) =>
              subscriptionProvider!..updateProfile(auth.profile),
        ),
      ],
      child: const PropertyAgentApp(),
    ),
  );
}
