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

  // raw.githubusercontent.com, not github.com/.../blob/... — GitHub's blob
  // viewer is a JS-heavy page that can render blank in stripped-down
  // in-app browsers (confirmed: blank/black in the emulator's browser) and
  // could do the same to an automated Play Store review crawler. Raw
  // content is plain text, needs no JS, and always renders. Works as a
  // public URL since the repo is public, but points at this feature
  // branch — once merged, repoint at the default-branch raw URL (or a
  // proper GitHub Pages / custom-domain page) so it survives the branch
  // being deleted.
  static const String privacyPolicyUrl =
      'https://raw.githubusercontent.com/frdsfrhn/Repo1/claude/flutter-firebase-mobile-app-h7fh9p/docs/PRIVACY_POLICY.md';

  static const String supportEmail = 'frdsfrhn@gmail.com';

  static const Duration maxRecordingDuration = Duration(minutes: 10);

  /// Must match the `region` passed to `setGlobalOptions()` in
  /// functions/src/index.ts. Cloud Functions callables default to
  /// us-central1 on the client side regardless of where the function is
  /// actually deployed — every `FirebaseFunctions` reference in the app
  /// needs to specify this region explicitly, or calls silently 404.
  static const String cloudFunctionsRegion = 'asia-southeast1';
}
