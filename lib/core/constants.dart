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

  // Rendered from docs/PRIVACY_POLICY.md on GitHub — works as a public
  // policy URL since the repo is public, but points at this feature
  // branch. Once merged, repoint this at the default-branch blob URL (or
  // a proper GitHub Pages / custom-domain page) so it survives the branch
  // being deleted.
  static const String privacyPolicyUrl =
      'https://github.com/frdsfrhn/Repo1/blob/claude/flutter-firebase-mobile-app-h7fh9p/docs/PRIVACY_POLICY.md';

  static const String supportEmail = 'frdsfrhn@gmail.com';

  static const Duration maxRecordingDuration = Duration(minutes: 10);

  /// Must match the `region` passed to `setGlobalOptions()` in
  /// functions/src/index.ts. Cloud Functions callables default to
  /// us-central1 on the client side regardless of where the function is
  /// actually deployed — every `FirebaseFunctions` reference in the app
  /// needs to specify this region explicitly, or calls silently 404.
  static const String cloudFunctionsRegion = 'asia-southeast1';
}
