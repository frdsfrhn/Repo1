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
        ChangeNotifierProvider<AuthProvider>(
          create: (_) => AuthProvider(
            authService: AuthService(),
            subscriptionService: subscriptionService,
          ),
        ),
        ChangeNotifierProvider<SubscriptionProvider>(
          create: (_) => SubscriptionProvider(),
        ),
      ],
      child: const PropertyAgentApp(),
    ),
  );
}
