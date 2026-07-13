import 'package:flutter/foundation.dart';

/// Build-time configuration, injected via --dart-define (see README "Run
/// the app" section) so RevenueCat's public SDK keys never get hardcoded
/// into source control.
class Env {
  Env._();

  static const String revenueCatApiKeyAndroid =
      String.fromEnvironment('REVENUECAT_API_KEY_ANDROID');
  static const String revenueCatApiKeyIos =
      String.fromEnvironment('REVENUECAT_API_KEY_IOS');

  static String get revenueCatApiKey {
    if (defaultTargetPlatform == TargetPlatform.iOS) {
      return revenueCatApiKeyIos;
    }
    return revenueCatApiKeyAndroid;
  }
}
