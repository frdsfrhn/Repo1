import 'package:purchases_flutter/purchases_flutter.dart';

import '../core/constants.dart';

/// Wraps RevenueCat for the subscription paywall and free-trial gating.
///
/// This only tells you the *purchase* state (has the AI-features
/// entitlement been bought). The Firestore `users/{uid}.subscriptionStatus`
/// field (kept in sync by the RevenueCat webhook — see
/// functions/src/revenuecatWebhook.ts) is what the rest of the app reads
/// for trial-vs-active-vs-expired UI, since it's readable without an SDK
/// round trip and is what Cloud Functions check server-side before
/// running AI work.
class SubscriptionService {
  bool _configured = false;

  Future<void> configure({required String apiKey, String? appUserId}) async {
    if (_configured) return;
    await Purchases.setLogLevel(LogLevel.warn);
    final configuration = PurchasesConfiguration(apiKey);
    if (appUserId != null) configuration.appUserID = appUserId;
    await Purchases.configure(configuration);
    _configured = true;
  }

  Future<void> logIn(String appUserId) async {
    if (!_configured) return;
    await Purchases.logIn(appUserId);
  }

  Future<void> logOut() async {
    if (!_configured) return;
    try {
      await Purchases.logOut();
    } catch (_) {
      // logOut() throws if already anonymous — safe to ignore on sign-out.
    }
  }

  Future<Offerings> getOfferings() => Purchases.getOfferings();

  Future<CustomerInfo> purchasePackage(Package package) async {
    final result = await Purchases.purchasePackage(package);
    return result.customerInfo;
  }

  Future<CustomerInfo> restorePurchases() => Purchases.restorePurchases();

  Future<bool> hasAiFeaturesEntitlement() async {
    final info = await Purchases.getCustomerInfo();
    return info.entitlements.active
        .containsKey(AppConstants.aiFeaturesEntitlementId);
  }
}
