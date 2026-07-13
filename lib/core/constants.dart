/// App-wide constants.
///
/// Trial/paywall numbers below are placeholders from the MVP checklist
/// ("14-day full trial" is the doc's suggested default) — confirm with
/// agents before shipping and adjust here. Nothing else in the app assumes
/// a particular value.
class AppConstants {
  AppConstants._();

  static const int trialLengthDays = 14;

  /// RevenueCat entitlement identifier that gates AI features (voice
  /// transcription + AI cleanup) — the app's real cost driver.
  static const String aiFeaturesEntitlementId = 'ai_features';

  /// RevenueCat offering identifier shown on the paywall.
  static const String defaultOfferingId = 'default';

  static const String privacyPolicyUrl =
      'https://example.com/privacy-policy'; // TODO: replace before submission

  static const String supportEmail = 'support@example.com'; // TODO: replace

  static const Duration maxRecordingDuration = Duration(minutes: 10);
}
